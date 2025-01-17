﻿using FreeScheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using ZhonTai.Admin.Core.Configs;

namespace ZhonTai.Admin.Tools.TaskScheduler;

public static class TaskSchedulerServiceExtensions
{
    public static IServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    /// 添加任务调度
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureOptions"></param>
    public static void AddTaskScheduler(this IServiceCollection services, Action<TaskSchedulerOptions> configureOptions = null)
    {
        ServiceProvider = services.BuildServiceProvider();
        var options = new TaskSchedulerOptions()
        {
            FreeSql = ServiceProvider.GetService<IFreeSql>()
        };
        configureOptions?.Invoke(options);

        var freeSql = options.FreeSql;

        freeSql.CodeFirst
        .ConfigEntity<TaskInfo>(a =>
        {
            a.Name("ad_task");
            a.Property(b => b.Id).IsPrimary(true);
            a.Property(b => b.Body).StringLength(-1);
            a.Property(b => b.Interval).MapType(typeof(string));
            a.Property(b => b.IntervalArgument).StringLength(1024);
            a.Property(b => b.Status).MapType(typeof(string));
            a.Property(b => b.CreateTime).ServerTime(DateTimeKind.Local);
            a.Property(b => b.LastRunTime).ServerTime(DateTimeKind.Local);
        })
        .ConfigEntity<TaskLog>(a =>
        {
            a.Name("ad_task_log");
            a.Property(b => b.Exception).StringLength(-1);
            a.Property(b => b.Remark).StringLength(-1);
            a.Property(b => b.CreateTime).ServerTime(DateTimeKind.Local);
        });

        options.ConfigureFreeSql?.Invoke(freeSql);

        var dbConfig = ServiceProvider.GetService<DbConfig>();
        if (dbConfig.SyncStructure)
        {
            freeSql.CodeFirst.SyncStructure<TaskInfo>();
            freeSql.CodeFirst.SyncStructure<TaskLog>();
        }

        if(options.TaskHandler != null)
        {
            //开启任务
            var scheduler = new Scheduler(options.TaskHandler);
            services.AddSingleton(scheduler);
        }
    }

    /// <summary>
    /// 使用任务调度
    /// </summary>
    /// <param name="app"></param>
    public static void UseTaskScheduler(this IApplicationBuilder app)
    {
        ServiceProvider = app.ApplicationServices;
    }
}
