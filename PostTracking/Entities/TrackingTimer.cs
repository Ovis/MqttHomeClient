using System;
using System.Timers;

namespace PostTracking.Entities
{
    internal class TrackingTimer
    {
        public TrackingTimer(string inquiryNumber, Action<Guid> handle)
        {
            InquiryNumber = inquiryNumber;

            SetDate = DateTime.UtcNow;

            TimerId = Guid.NewGuid();

            Timer = new Timer
            {
                Interval = 120000,
                AutoReset = true,
            };

            Timer.Elapsed += (_, __) => handle(TimerId);

            Timer.Enabled = true;
        }

        /// <summary>
        /// タイマー
        /// </summary>
        public Timer Timer { get; set; }


        /// <summary>
        /// 問い合わせ番号
        /// </summary>
        public string InquiryNumber { get; set; }


        public Guid TimerId { get; }

        public DateTime SetDate { get; }
    }
}
