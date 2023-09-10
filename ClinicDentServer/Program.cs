using ClinicDentServer.SocketServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ClinicDentServer
{
    public class Program
    {
        public static Server TcpServer;
        public static void Main(string[] args)
        {
            TcpServer = new Server();
            TcpServer.Start();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseIISIntegration();
                });
    }
}
