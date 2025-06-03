namespace Geex.Extensions.Messaging
{
    public enum MessageType
    {
        /// <summary>
        /// 通知, 告知某个信息的消息
        /// 区别于单独的toast, 这个消息会留档
        /// </summary>
        Notification,
        /// <summary>
        /// 待办, 带有链接跳转/当前状态等交互功能的消息
        /// </summary>
        Todo,
        /// <summary>
        /// 用户交互消息, 通常有一个非系统的触发者
        /// </summary>
        Interact
    }
}
