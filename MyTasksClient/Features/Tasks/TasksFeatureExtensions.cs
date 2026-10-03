using Microsoft.Extensions.DependencyInjection;
using MyTasksClient.Features.Tasks.ViewModels;
using MyTasksClient.Features.Tasks.Views;
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
            services.AddTransient<TasksViewModel>();
            services.AddTransient<TasksPage>();
            return services;
        }
    }
}
