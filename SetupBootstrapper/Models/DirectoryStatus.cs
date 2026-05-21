namespace SetupBootstrapper.Models
{
    public enum DirectoryStatus
    {
        /// <summary>目标文件夹为空或不存在</summary>
        Empty,
        /// <summary>检测到旧版本/便携版</summary>
        OldVersion,
        /// <summary>存在其他不相关文件</summary>
        HasOtherFiles,
        /// <summary>当前权限不足，需要管理员权限</summary>
        RequiresAdmin
    }
}
