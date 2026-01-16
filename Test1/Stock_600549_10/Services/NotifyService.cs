using Newtonsoft.Json;
using StockTenDayLineWarning.Models.Entities;
using System.Net;
using System.Net.Mail;

namespace StockTenDayLineWarning.Services
{
    public class NotifyService : INotifyService
    {
        private readonly HttpClient _httpClient;

        public NotifyService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        }

        #region 邮件预警（保留）
        public async Task SendEmailWarningAsync(StockWarning warning)
        {
            try
            {
                using var smtp = new SmtpClient("smtp.qq.com", 587);
                smtp.Credentials = new NetworkCredential("你的QQ邮箱", "你的QQ邮箱SMTP授权码");
                smtp.EnableSsl = true;

                var mail = new MailMessage
                {
                    From = new MailAddress("你的QQ邮箱"),
                    Subject = $"【{warning.WarningType}预警】{warning.StockName}({warning.StockCode})",
                    Body = $"触发时间：{warning.TriggerTime:yyyy-MM-dd HH:mm}<br>预警价格：{warning.WarningPrice:F2}<br>10日均线：{warning.TenDayAverage:F2}<br>触发量能：{warning.TriggerVolume:N0}手",
                    IsBodyHtml = true
                };
                mail.To.Add("接收预警的邮箱");

                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"【{DateTime.Now}】邮件预警发送失败：{ex.Message}");
            }
        }
        #endregion

        #region 微信预警（核心实现）
        public async Task SendWeChatWarningAsync(StockWarning warning)
        {
            try
            {
                // 步骤1：获取微信 access_token
                var accessToken = await GetWeChatAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine($"【{DateTime.Now}】微信预警失败：获取access_token为空");
                    return;
                }

                // 步骤2：构造微信文本消息
                var weChatMessage = new
                {
                    touser = WeChatConfig.OpenID,
                    msgtype = "text",
                    text = new
                    {
                        content = $"【{warning.WarningType}预警】\n股票名称：{warning.StockName}\n股票代码：{warning.StockCode}\n预警价格：{warning.WarningPrice:F2}\n10日均线：{warning.TenDayAverage:F2}\n触发量能：{warning.TriggerVolume:N0}手\n触发时间：{warning.TriggerTime:yyyy-MM-dd HH:mm}\n\n（10日线战法+量价配合精准信号）"
                    }
                };

                // 步骤3：调用微信接口发送消息
                var sendUrl = string.Format(WeChatConfig.SendMessageUrl, accessToken);
                var jsonContent = new StringContent(JsonConvert.SerializeObject(weChatMessage), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(sendUrl, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"【{DateTime.Now}】微信预警发送失败：{errorMsg}");
                    return;
                }

                Console.WriteLine($"【{DateTime.Now}】微信预警发送成功：{warning.StockName}({warning.StockCode}) {warning.WarningType}信号");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"【{DateTime.Now}】微信预警发送异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 辅助：获取微信 access_token
        /// </summary>
        private async Task<string> GetWeChatAccessTokenAsync()
        {
            try
            {
                var requestUrl = string.Format(WeChatConfig.GetAccessTokenUrl, WeChatConfig.AppID, WeChatConfig.AppSecret);
                var response = await _httpClient.GetAsync(requestUrl);
                var jsonStr = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<WeChatAccessTokenResult>(jsonStr);
                if (result == null || string.IsNullOrEmpty(result.access_token))
                {
                    Console.WriteLine($"【{DateTime.Now}】获取微信access_token失败：{jsonStr}");
                    return string.Empty;
                }

                return result.access_token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"【{DateTime.Now}】获取微信access_token异常：{ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 微信 access_token 返回数据模型
        /// </summary>
        private class WeChatAccessTokenResult
        {
            public string access_token { get; set; } = string.Empty;
            public int expires_in { get; set; }
            public string errcode { get; set; } = string.Empty;
            public string errmsg { get; set; } = string.Empty;
        }
        #endregion
    }
}