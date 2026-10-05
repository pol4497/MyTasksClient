using Microsoft.Extensions.DependencyInjection;
using MyTasksClient.Features.Tasks.Services;
using MyTasksClient.Features.Tasks.ViewModels;
using MyTasksClient.Features.Tasks.Views;
using MyTasksClient.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyTasksClient.Features.Tasks
{
    public static class TasksFeatureExtensions
    {
        /// <summary>Registers everything the Tasks feature needs.</summary>
        public static IServiceCollection AddTasksFeature(this IServiceCollection services)
        {
            services.AddHttpClient<ITasksApiClient, TasksApiClient>(client =>
            {
                client.BaseAddress = ApiConfiguration.GetBaseAddress();
                client.Timeout = TimeSpan.FromSeconds(15);
            });

            services.AddTransient<TaskQueryViewModel>();
            services.AddTransient<AddTaskViewModel>();
            services.AddTransient<TasksViewModel>();
            services.AddTransient<TasksPage>();

            return services;
        }
    }
}
