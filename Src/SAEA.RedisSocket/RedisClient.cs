﻿/****************************************************************************
*Copyright (c) 2018 Microsoft All Rights Reserved.
*CLR版本： 4.0.30319.42000
*机器名称：WENLI-PC
*公司名称：Microsoft
*命名空间：SAEA.RedisSocket
*文件名： RedisOperation
*版本号： V1.0.0.0
*唯一标识：23cf910b-3bed-4d80-9e89-92c04fba1e5e
*当前的用户域：WENLI-PC
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2018/3/16 10:12:40
*描述：
*
*=====================================================================
*修改标记
*修改时间：2018/3/16 10:12:40
*修改人： yswenli
*版本号： V1.0.0.0
*描述：
*
*****************************************************************************/
using SAEA.Commom;
using SAEA.RedisSocket.Core;
using SAEA.RedisSocket.Interface;
using SAEA.RedisSocket.Model;
using SAEA.RedisSocket.Net;
using System;
using System.Threading;

namespace SAEA.RedisSocket
{
    public class RedisClient : IClient, IDisposable
    {
        RedisConnection _cnn;

        RedisDataBase _redisDataBase;

        object _syncLocker = new object();

        int _dbIndex = 0;

        public bool IsConnected { get; set; }

        public RedisClient(string ipPort, bool debugModel = false)
        {
            _cnn = new RedisConnection(ipPort, debugModel);

        }

        /// <summary>
        /// 连接到redis服务器
        /// </summary>
        public void Connect()
        {
            lock (_syncLocker)
            {
                if (!IsConnected)
                {
                    _cnn.Connect();

                    IsConnected = _cnn.IsConnected;                    
                }
            }
        }
        /// <summary>
        /// 使用密码连接到RedisServer
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public string Connect(string password)
        {
            lock (_syncLocker)
            {
                var ok = "OK";

                if (!IsConnected)
                {
                    _cnn.Connect();

                    IsConnected = _cnn.IsConnected;

                    var infoMsg = Info();

                    if (infoMsg.Contains("NOAUTH Authentication required."))
                    {
                        if (string.IsNullOrEmpty(password))
                        {
                            _cnn.Quit();
                            return infoMsg;
                        }

                        var authMsg = Auth(password);                        

                        if (!authMsg.Contains(ok))
                        {
                            _cnn.Quit();
                            return authMsg;
                        }                        
                    }                    
                }
                return ok;
            }
        }



        /// <summary>
        /// 保持redis连接
        /// </summary>
        private void KeepAlive()
        {
            ThreadHelper.Run(() =>
            {
                while (IsConnected)
                {
                    if (_cnn.Actived <= DateTimeHelper.Now)
                    {
                        Ping();
                    }
                    ThreadHelper.Sleep(10 * 1000);
                }
            }, true, ThreadPriority.Highest);
        }

        /// <summary>
        /// redis 密码验证
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public string Auth(string password)
        {
            return GetDataBase().Do(RequestType.AUTH, password).Data;
        }

        /// <summary>
        /// redis ping
        /// </summary>
        /// <returns></returns>
        public string Ping()
        {
            return GetDataBase().Do(RequestType.PING).Data;
        }

        /// <summary>
        /// 选择redis database
        /// </summary>
        /// <param name="dbIndex"></param>
        /// <returns></returns>
        public string Select(int dbIndex = -1)
        {
            if (dbIndex > -1)
            {
                _dbIndex = dbIndex;
            }
            return GetDataBase().Do(RequestType.SELECT, _dbIndex.ToString()).Data;
        }
        /// <summary>
        /// 当前db数据项数据
        /// </summary>
        /// <returns></returns>
        public int DBSize()
        {
            var result = 0;
            int.TryParse(GetDataBase().Do(RequestType.DBSIZE).Data, out result);
            return result;
        }
        /// <summary>
        /// 获取某个集合的类型 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Type(string key)
        {
            return GetDataBase().Do(RequestType.TYPE, key).Data;
        }
        /// <summary>
        /// redis server的信息
        /// </summary>
        /// <returns></returns>
        public string Info()
        {
            return GetDataBase().Do(RequestType.INFO).Data;
        }

        /// <summary>
        /// 设置或取消丛
        /// </summary>
        /// <param name="ipPort">格式：ip port，若为空则为取消</param>
        /// <returns></returns>
        public string SlaveOf(string ipPort = "")
        {
            return GetDataBase().Do(RequestType.SLAVEOF, string.IsNullOrEmpty(ipPort) ? "NO ONE" : ipPort).Data;
        }
        /// <summary>
        /// redis server是否是主
        /// </summary>
        /// <returns></returns>
        public bool IsMaster()
        {
            var info = Info();
            if (info.Contains("role:master"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取redis database操作
        /// </summary>
        /// <param name="dbIndex"></param>
        /// <returns></returns>
        public RedisDataBase GetDataBase(int dbIndex = -1)
        {
            lock (_syncLocker)
            {
                if (dbIndex > 0 && dbIndex != _dbIndex)
                {
                    _dbIndex = dbIndex;
                    Select(_dbIndex);
                }
                if (_redisDataBase == null)
                {
                    _redisDataBase = new RedisDataBase(_cnn);
                }
                return _redisDataBase;
            }

        }

        public void Dispose()
        {
            _cnn.Dispose();
        }
    }
}
