using System;

namespace Game
{
    public class CachedTimerPref : PlayerPrefSingleton<CachedTimerPref, CachedTimerCachedData>
    {
        public DateTime NextDay { get; private set; }
        public DateTime NextWeek { get; private set; }
        public DateTime NextMonth { get; private set; }

        protected override int GetCurrentVersion() => 1;
        public override string GetKeyData() => "cached_timer";
        public override string GetKeyEvent() => Constant.EVENT_ON_CACHED_TIMER_UPDATED;

        protected override void OnInitSuccess()
        {
            base.OnInitSuccess();
            NextDay = DateTimeHelper.ParseUnixTimestampNormal(cachedData.nextDay);
            NextWeek = DateTimeHelper.ParseUnixTimestampNormal(cachedData.nextWeek);
            NextMonth = DateTimeHelper.ParseUnixTimestampNormal(cachedData.nextMonth);
        }

        protected override void OnNewSecond()
        {
            base.OnNewSecond();
            if (IsNewDay()) TigerForge.EventManager.EmitEvent(Constant.EVENT_ON_NEW_DAY);
            if (IsNewWeek()) TigerForge.EventManager.EmitEvent(Constant.EVENT_ON_NEW_WEEK);
            if (IsNewMonth()) TigerForge.EventManager.EmitEvent(Constant.EVENT_ON_NEW_MONTH);
        }

        protected override void OnNextDay()
        {
            base.OnNextDay();
            NextDay = DateTime.UtcNow.Date.AddDays(1);
            cachedData.nextDay = NextDay.ToUnixTimestamp();
            OnValueChange();
        }

        protected override void OnNextWeek()
        {
            base.OnNextWeek();
            DateTime dateTime = DateTime.UtcNow;
            int daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)dateTime.DayOfWeek + 7) % 7;
            NextWeek = dateTime.Date.AddDays(daysUntilNextMonday == 0 ? 7 : daysUntilNextMonday);
            cachedData.nextWeek = NextWeek.ToUnixTimestamp();
            OnValueChange();
        }

        protected override void OnNextMonth()
        {
            base.OnNextMonth();
            DateTime dateTime = DateTime.UtcNow;
            DateTime nextMonthDt = dateTime.AddMonths(1);
            NextMonth = new DateTime(nextMonthDt.Year, nextMonthDt.Month, 1);
            cachedData.nextMonth = NextMonth.ToUnixTimestamp();
            OnValueChange();
        }

        public bool IsNewDay()
        {
            return DateTime.UtcNow > NextDay;
        }

        public bool IsNewWeek()
        {
            return DateTime.UtcNow > NextWeek;
        }

        public bool IsNewMonth()
        {
            return DateTime.UtcNow > NextMonth;
        }
    }
    
    [Serializable]
    public class CachedTimerCachedData : PlayerPrefCachedData
    {
        public long nextDay;
        public long nextWeek;
        public long nextMonth;

        public override void OnNewData()
        {
        }

        public override void FirstTimeInit()
        {
        }
    }
}