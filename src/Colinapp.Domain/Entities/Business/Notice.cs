using Colinapp.Domain.Enums;

namespace Colinapp.Domain.Entities.Business;

/// <summary>
/// 公告（业务扩展样例）。继承审计基类，自动获得软删除、审计与租户字段。
/// 演示「独立业务模块」如何融入框架：建实体 → EF 配置 → 服务 → 控制器 → 挂菜单 → 前端页面。
/// </summary>
public class Notice : EntityBase
{
    /// <summary>标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>类型：通知/公告</summary>
    public NoticeType NoticeType { get; set; } = NoticeType.Notification;

    /// <summary>内容（富文本/纯文本）</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>是否已发布</summary>
    public bool Published { get; set; }
}
