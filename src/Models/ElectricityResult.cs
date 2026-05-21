using System;

namespace cpu_net.Model
{
    /// <summary>
    /// 电费查询结果
    /// </summary>
    public class ElectricityResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 剩余电费金额（元）
        /// </summary>
        public decimal? Balance { get; set; }

        /// <summary>
        /// 剩余可用电量（度）
        /// </summary>
        public decimal? Degrees { get; set; }

        /// <summary>
        /// 是否未绑定房间
        /// </summary>
        public bool IsRoomNotBound { get; set; }

        /// <summary>
        /// 房间信息
        /// </summary>
        public string? RoomInfo { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 原始响应内容（用于调试）
        /// </summary>
        public string? RawResponse { get; set; }

        /// <summary>
        /// 查询时间
        /// </summary>
        public DateTime QueryTime { get; set; } = DateTime.Now;
    }
}
