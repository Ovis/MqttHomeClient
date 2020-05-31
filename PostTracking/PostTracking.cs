using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PluginInterface;
using PostTracking.Domain;
using PostTracking.Entities;

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


        public async Task<Result> ActionAsync(string inquiryNumber)
        {
            _track ??= new Tracking();

            if (_config == null)
            {
                _config = new Config();
                _config = _config.GetConfig();

                if (_config == null)
                {
                    return new Result
                    {
                        Status = ResultStatus.FailedOnApp,
                        Message = $"PostTrackingのコンフィグファイル読み込みに失敗しました。"
                    };
                }
            }

            var trackTime = new TrackingTimer(inquiryNumber, Elapsed);

            _timerList.Add(trackTime);

            return new Result
            {
                Status = ResultStatus.SuccessOnAppHasMessage,
                Message = $"配送番号{inquiryNumber}の配送監視を設定しました。"
            };
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

            if (isFinish)
            {
                var message = $"追跡番号{tracking.InquiryNumber}の荷物は{finishDate}に配達が完了しました。";
                await _track.PostWebHook(_config.WebHookUrl, message);
                Console.Write("お届け済みのため終了しました。");

                tracking.Timer.Stop();
            }
            else if (DateTime.UtcNow > tracking.SetDate.AddDays(5))
            {
                var message = $"追跡番号{tracking.InquiryNumber}の荷物は所定時間を経過しても配送完了にならないため、監視を終了しました。";
                await _track.PostWebHook(_config.WebHookUrl, message);
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