﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args)
            .Build()
            .Start();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseStartup<ReactiveServicesStartup>();
}

