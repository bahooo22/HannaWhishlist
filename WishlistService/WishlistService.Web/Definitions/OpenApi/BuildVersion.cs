namespace WishlistService.Web.Definitions.OpenApi;

public static class BuildVersion
{
    private static readonly string _filePath = Path.Combine(AppContext.BaseDirectory, "build.version");

    public static string Current
    {
        get
        {
            // z — счётчик билдов
            var build = File.Exists(_filePath) ? File.ReadAllText(_filePath).Trim() : "0";

            // x — мажорная версия (задаёшь руками)
            var major = 1;

            // y — можно взять короткий SHA из GitInfo или пока фиксировать вручную
#if DEBUG
            var minor = "dev"; // для локальных билдов
#else
            var minor = ThisAssembly.Git.Commit.Substring(0, 7); // для коммитов
#endif

            return $"{major}.{minor}.{build}";
        }
    }
}
