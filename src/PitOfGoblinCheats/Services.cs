using Il2CppInterop.Runtime.InteropTypes;

namespace PitOfGoblinCheats;

internal static class Services
{
    public static T Find<T>() where T : Il2CppObjectBase
    {
        var services = ServiceLocator.m_serviceList;
        if (services == null) return null;

        for (int i = 0; i < services.Count; i++)
        {
            var found = services[i]?.TryCast<T>();
            if (found != null) return found;
        }
        return null;
    }
}
