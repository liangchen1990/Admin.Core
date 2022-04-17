﻿using Autofac;
using Autofac.Extras.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Module = Autofac.Module;
using ZhonTai.Common.Domain;
using ZhonTai.Common.Configs;
using Microsoft.Extensions.DependencyModel;

namespace ZhonTai.Admin.Core.RegisterModules
{
    public class ServiceModule : Module
    {
        private readonly AppConfig _appConfig;
        private readonly string _assemblySuffixName;

        /// <summary>
        /// 服务注入
        /// </summary>
        /// <param name="appConfig">AppConfig</param>
        /// <param name="assemblySuffixName">程序集后缀名</param>
        public ServiceModule(AppConfig appConfig, string assemblySuffixName = "Service")
        {
            _appConfig = appConfig;
            _assemblySuffixName = assemblySuffixName;
        }

        protected override void Load(ContainerBuilder builder)
        {
            //事务拦截
            var interceptorServiceTypes = new List<Type>();
            if (_appConfig.Aop.Transaction)
            {
                builder.RegisterType<TransactionInterceptor>();
                builder.RegisterType<TransactionAsyncInterceptor>();
                interceptorServiceTypes.Add(typeof(TransactionInterceptor));
            }

            //服务
            Assembly[] assemblies = DependencyContext.Default.RuntimeLibraries
                .Where(a => a.Name.EndsWith(_assemblySuffixName))
                .Select(o => Assembly.Load(new AssemblyName(o.Name))).ToArray();

            //服务接口实例
            builder.RegisterAssemblyTypes(assemblies)
            .Where(a => a.Name.EndsWith(_assemblySuffixName))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope()
            .PropertiesAutowired()// 属性注入
            .InterceptedBy(interceptorServiceTypes.ToArray())
            .EnableInterfaceInterceptors();

            //服务实例
            builder.RegisterAssemblyTypes(assemblies)
            .Where(a => a.Name.EndsWith(_assemblySuffixName))
            .InstancePerLifetimeScope()
            .PropertiesAutowired()// 属性注入
            .InterceptedBy(interceptorServiceTypes.ToArray())
            .EnableClassInterceptors();
        }
    }
}