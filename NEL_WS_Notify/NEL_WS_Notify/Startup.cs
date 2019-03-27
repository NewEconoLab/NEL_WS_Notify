using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NEL_WS_Notify.Notify;

namespace NEL_WS_Notify
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("mongodbsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors(builder =>
                builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMvc();

            initWsProcessor(app);
        }

        private void initWsProcessor(IApplicationBuilder app)
        {
            // 初始化参数
            initParam();
            
            // 初始化连接
            var options = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024,
            };
            app.UseWebSockets(options);
            app.Use(async (context, next) => {
                if (context.Request.Path == "/ws/testnet" && context.WebSockets.IsWebSocketRequest)
                {
                    var ws = await context.WebSockets.AcceptWebSocketAsync();
                    await NotifyProcessor.initWsProcessor(context, ws, "testnet");
                }
                if (context.Request.Path == "/ws/mainnet" && context.WebSockets.IsWebSocketRequest)
                {
                    var ws = await context.WebSockets.AcceptWebSocketAsync();
                    await NotifyProcessor.initWsProcessor(context, ws, "mainnet");
                }
            });


            // 启动任务
            Task.Run(() => NotifyProcessor.loop());

            // 启动心跳
            Task.Run(() => NotifyProcessor.ping());
        }

        private void initParam()
        {
            WsConst.block_mongodbConnStr_testnet = Configuration.GetSection("mongodbConnStr_testnet").Value;
            WsConst.block_mongodbDatabase_testnet = Configuration.GetSection("mongodbDatabase_testnet").Value;
            WsConst.block_mongodbConnStr_mainnet = Configuration.GetSection("mongodbConnStr_mainnet").Value;
            WsConst.block_mongodbDatabase_mainnet = Configuration.GetSection("mongodbDatabase_mainnet").Value;
            WsConst.ping_interval_minutes = int.Parse(Configuration.GetSection("ping_interval_minutes").Value);
            WsConst.loop_interval_seconds = int.Parse(Configuration.GetSection("loop_interval_seconds").Value);
        }
    }
}
