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
            builder.Add(new DeviceWitStatus(new Device($"This device ({currentIp})", currentIp), status.ContainsKey(currentIp)));

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
                await _updateEndpointClient.GetAsync(ConfigHelper.Instance.HitWhenIpChanges);
            }
            
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

            _lastKnownIp = currentIp;
            var response = await _updateEndpointClient.GetAsync(ConfigHelper.Instance.HitWhenIpChanges);
            var content = await response.Content.ReadAsStringAsync();
            return Content(content);
        }
    }
}
