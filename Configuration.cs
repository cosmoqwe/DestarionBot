using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DestarionBot
{
    public static class Configuration 
    {
        private static readonly IConfigurationRoot _root;
        static Configuration()
        {
            var builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _root = builder.Build();
        }
        public static IConfigurationRoot Config
        {
            get => _root;
        }
    }
}
