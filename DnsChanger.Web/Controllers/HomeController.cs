using System.Collections.Immutable;
using System.Linq;
using DnsChanger.Core;
using DnsChanger.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DnsChanger.Web.Controllers
{
    public class HomeController : Controller
    {
        public HomeController(IConfiguration configuration)
        {
            ConfigHelper.Init(configuration.GetValue<string>("CONFIG_PATH"));
        }

        [Route("")]
        public IActionResult Index()
        {
            var status = GatewayHelper.GetRedirectedIps();
            var builder = ImmutableArray.CreateBuilder<DeviceWitStatus>();
            foreach (var device in ConfigHelper.Instance.Devices.OrderBy(d => d.Name))
            {
                builder.Add(new DeviceWitStatus(device, status.ContainsKey(device.Ip)));
            }

            var model = new IndexModel(builder.ToImmutable(), GatewayHelper.GetCurrentIp());
            return View(model);
        }

        [Route("change")]
        public IActionResult Change(bool redirect, string ip)
        {
            if (redirect)
            {
                GatewayHelper.AddRedirection(ip);
            }
            else
            {
                GatewayHelper.DeleteRedirection(ip);
            }

            return Redirect("/");
        }

        [Route("change-wan-ip")]
        public IActionResult ChangeWanIp()
        {
            GatewayHelper.ChangeWanIp();
            return new EmptyResult();
        }
    }
}
