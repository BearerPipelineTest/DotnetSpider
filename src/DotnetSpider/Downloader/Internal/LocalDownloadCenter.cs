using System.Linq;
using System.Threading.Tasks;
using DotnetSpider.Core;
using DotnetSpider.Downloader.Entity;
using DotnetSpider.MessageQueue;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DotnetSpider.Downloader.Internal
{
	/// <summary>
	/// 本地下载中心
	/// </summary>
	public class LocalDownloadCenter : DownloadCenterBase
	{
		/// <summary>
		/// 构造方法
		/// </summary>
		/// <param name="mq">消息队列</param>
		/// <param name="downloaderAgentStore">下载器代理存储</param>
		/// <param name="logger">日志接口</param>
		public LocalDownloadCenter(IMessageQueue mq,
			IDownloaderAgentStore downloaderAgentStore,
			ILogger<LocalDownloadCenter> logger) : base(mq, downloaderAgentStore, logger)
		{
		}

		/// <summary>
		/// 单机模式只有一个下载器代理
		/// </summary>
		/// <param name="allotDownloaderMessage">分配下载器代理的选项</param>
		/// <returns></returns>
		protected override async Task<bool> AllocateAsync(AllotDownloaderMessage allotDownloaderMessage)
		{
			var agent = Agents.Values.FirstOrDefault();
			if (agent == null)
			{
				Logger.LogInformation($"任务 {allotDownloaderMessage.OwnerId} 未找到可用的下载器代理");
				return false;
			}

			// 保存节点选取信息
			await DownloaderAgentStore.AllocateAsync(allotDownloaderMessage.OwnerId, new[] {agent.Id});
			// 发送消息让下载代理器分配好下载器
			var message =
				$"|{Framework.AllocateDownloaderCommand}|{JsonConvert.SerializeObject(allotDownloaderMessage)}";

			await Mq.PublishAsync(agent.Id, message);
			Logger.LogInformation(
				$"任务 {allotDownloaderMessage.OwnerId} 分配下载代理器成功: {JsonConvert.SerializeObject(agent)}");
			return true;
		}
	}
}