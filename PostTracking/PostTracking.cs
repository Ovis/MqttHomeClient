using PluginInterface;
using PostTracking.Domain;
using PostTracking.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostTracking
{
    public class PostTracking : IPlugin
    {
        public string Topic { get; } = "PostTracking";

        public string PluginName { get; } = "PostTracking";

        public bool IsAsync { get; } = false;

        private readonly List<TrackingTimer> _timerList = new List<TrackingTimer>();

        private Tracking _track;

        private Config _config;


        public bool Action(string inquiryNumber)
        {
            _track ??= new Tracking();

            if (_config == null)
            {
                _config = new Config();
                _config = _config.GetConfig();

                if (_config == null)
                {
                    Console.WriteLine("PostTrackingのコンフィグファイル読み込みに失敗しました。");
                }
            }

            var trackTime = new TrackingTimer(inquiryNumber, Elapsed);

            _timerList.Add(trackTime);

            Console.WriteLine($"配送番号{inquiryNumber}の配送監視を設定しました。");
            Console.WriteLine(_config.WebHookUrl);

            return false;
        }


        public Task<bool> ActionAsync(string text)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// タイマー発火時処理
        /// </summary>
        /// <param name="guid"></param>
        public async void Elapsed(Guid guid)
        {
            var tracking = _timerList.FirstOrDefault(u => u.TimerId == guid);

            if (tracking == null) return;

            var (isFinish, finishDate) = await _track.CheckPost(tracking.InquiryNumber);

            if (isFinish || DateTime.UtcNow > tracking.SetDate.AddDays(5))
            {
                await _track.PostWebHook(_config.WebHookUrl, finishDate);
                Console.Write("お届け済みのため終了しました。");

                tracking.Timer.Stop();
            }
        }

        /// <summary>
        /// プラグイン側処理を終了させる
        /// </summary>
        /// <returns></returns>
        public bool QuitAction()
        {
            foreach (var timer in _timerList)
            {
                timer.Timer.Stop();
                timer.Timer.Dispose();
            }

            return true;
        }
    }
}