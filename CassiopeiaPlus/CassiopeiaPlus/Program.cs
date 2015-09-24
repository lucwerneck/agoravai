using EloBuddy.SDK.Events;

namespace CassiopeiaPlus
{
    class Program
    {
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += eventArgs => Cassiopeia.OnLoad();
        }
    }
}
