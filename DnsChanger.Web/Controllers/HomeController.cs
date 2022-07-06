using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DnsChanger.Core;
using DnsChanger.Core.Models;
using DnsChanger.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DnsChanger.Web.Controllers
{
    public class HomeController : Controller
    {
        private static string _lastKnownIp;
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

            var currentIp = Request.HttpContext.Connection.RemoteIpAddress.ToString();
            if (!currentIp.Contains(":"))
            {
                builder.Add(new DeviceWitStatus(new Device($"This device", currentIp),
                    status.ContainsKey(currentIp)));
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
        public async Task<IActionResult> ChangeWanIp()
        {
            GatewayHelper.ChangeWanIp();
            if (ConfigHelper.Instance.HitWhenIpChanges.HasValue())
            {
                var urls = ConfigHelper.Instance.HitWhenIpChanges.Split(",").Select(u => u.Trim());
                foreach (var url in urls)
                {
                    try
                    {
                        var r = await _updateEndpointClient.GetAsync(url);
                        r.EnsureSuccessStatusCode();
                    }
                    catch (Exception e)
                    {
                        return Content($"Error when trying to update the ip at {url}. It will be automatically retried in 5 minutes. Exception: {e.Message}");
                    }
                }
            }

            _lastKnownIp = GatewayHelper.GetCurrentIp();
            return RedirectToAction(nameof(Index));
        }

        private static HttpClient _updateEndpointClient = new HttpClient();
        [Route("update-endpoint-if-changed")]
        public async Task<IActionResult> UpdateEndpointIfChanged()
        {
            if (ConfigHelper.Instance.HitWhenIpChanges.IsNullOrEmpty())
            {
                return new EmptyResult();
            }

            var currentIp = GatewayHelper.GetCurrentIp();
            if (currentIp == _lastKnownIp)
            {
                return new EmptyResult();
            }
            
            var urls = ConfigHelper.Instance.HitWhenIpChanges.Split(",").Select(u => u.Trim());
            var content = "";
            foreach (var url in urls)
            {
                HttpResponseMessage response;
                try
                {
                    response = await _updateEndpointClient.GetAsync(ConfigHelper.Instance.HitWhenIpChanges);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception e)
                {
                    return Content($"Error when trying to update the ip at {url}. It will be automatically retried in 5 minutes. Exception: {e.Message}");
                }

                content += "url: " + url + "\n";
                content += await response.Content.ReadAsStringAsync();
                content += "\n-----\n";
            }
            
            _lastKnownIp = currentIp;
            return Content(content);
        }
    }
}
