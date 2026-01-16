namespace StockTenDayLineWarning.Services
{
    /// <summary>
    /// 微信预警配置（免费测试号）
    /// </summary>
    public static class WeChatConfig
    {
        // 替换为你的微信测试号 AppID
        public const string AppID = "你的测试号AppID（如wx1234567890abcdef）";

        // 替换为你的微信测试号 AppSecret
        public const string AppSecret = "你的测试号AppSecret（如1234567890abcdef1234567890abcdef）";

        // 替换为你的微信 openid（关注测试号后获取）
        public const string OpenID = "你的个人微信openid（如o6_bmjrPTlm6_2sgVt7hMZOPfL2M）";

        // 微信接口地址（免费公开，无需修改）
        /// 获取access_token接口（微信接口调用凭证）
        public const string GetAccessTokenUrl = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}";

        /// 发送模板消息/文本消息接口
        public const string SendMessageUrl = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token={0}";
    }
}