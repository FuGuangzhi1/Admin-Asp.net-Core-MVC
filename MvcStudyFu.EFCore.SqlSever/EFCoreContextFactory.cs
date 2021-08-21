﻿using MvcStudyFu.EFCore.SQLSever;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using StudyMVCFu.Model;

namespace MvcStudyFu.EFCore.SQLSever
{
    public class EFCoreContextFactory : IDbContextFactory
    {
        private readonly IConfiguration _configuration;
        private static DBConnectionOption _dBConnectionOption;
        private static StudyMVCDBContext _context = null;
        private static bool b = true;
        public EFCoreContextFactory(IConfiguration configuration)
        {
            this._configuration = configuration;
            if (_configuration["ConnectionStrings:Write"]==null)
            {
                throw new Exception("请设置配置文件");
            }
            _dBConnectionOption = new DBConnectionOption()
            {
                MainConnectionString = _configuration["ConnectionStrings:Write"],
                SlaveConnectionStringList = new List<string> {
                    _configuration["ConnectionStrings:Read:0"],
                    _configuration["ConnectionStrings:Read:1"], }
            };
            if (b) { Create(); b = false; }
        }
        public static StudyMVCDBContext CreateDbContext()
        {
            return new StudyMVCDBContext(_dBConnectionOption.MainConnectionString);
        }
        public StudyMVCDBContext CreateDbContext(ReadWriteEnum writeOrRead)
        {
            var whetherToSeparateReadingAndWriting = _configuration.GetSection("WhetherToSeparateReadingAndWriting");
            if (!whetherToSeparateReadingAndWriting.Exists())
            {
                if (!bool.TryParse(whetherToSeparateReadingAndWriting.Value,out _))
                {
                return new StudyMVCDBContext(_dBConnectionOption.MainConnectionString);
                }
            }
            try
            {
                switch (writeOrRead)
                {
                    case ReadWriteEnum.Write:
                        _context = new StudyMVCDBContext(_dBConnectionOption.MainConnectionString);
                        break;
                    //主库连接 
                    case ReadWriteEnum.Read:
                        _context = new StudyMVCDBContext(GetReadConnect());
                        //从库连接
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
                return new StudyMVCDBContext(_dBConnectionOption.MainConnectionString);
            }
            return _context;
        }

        //1,当前请求数量
        private static int _currentRequestCount = 0;
        private static string GetReadConnect()
        {
            //定义一个轮询策略
            //根据请求量来取模
            int currentIndex = _currentRequestCount % _dBConnectionOption.SlaveConnectionStringList.Count;
            _currentRequestCount++;
            return _dBConnectionOption.SlaveConnectionStringList[currentIndex];

            //定义一个随机策略
            // int i = new Random().Next(0, strConns.Count);
            // return strConns[i];

        }
        /// <summary>
        /// 创建数据库，不存在的话
        /// </summary>
        private static void Create()
        {
            _context = new StudyMVCDBContext(_dBConnectionOption.MainConnectionString);
            _context.Database.EnsureCreated();

        }

    }
}