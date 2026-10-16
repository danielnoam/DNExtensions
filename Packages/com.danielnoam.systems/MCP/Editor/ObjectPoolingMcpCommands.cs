using System.Text;
using DNExtensions.Systems.ObjectPooling;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.MCP
{
    internal static class ObjectPoolingMcpCommands
    {
        [CliCommand("dn_list_pools", "List the object pools from ObjectPoolingSettings, with live active/inactive counts while in play mode")]
        private static string ListPools()
        {
            ObjectPoolingSettings settings = ObjectPoolingSettings.Instance;

            if (!settings)
            {
                return "No ObjectPoolingSettings asset in a Resources folder.";
            }

            var builder = new StringBuilder($"Object Pooling{(settings.enabled ? "" : " (disabled)")}, instantiate as fallback: {settings.instantiateAsFallback}, destroy as fallback: {settings.destroyAsFallback}\n");

            if (Application.isPlaying && ObjectPooler.Instance)
            {
                builder.AppendLine("Runtime pools:");

                foreach (Pool pool in ObjectPooler.Instance.Pools)
                {
                    builder.AppendLine($"  {pool.PoolName}: {pool.ActiveCount} active, {pool.InactiveCount} inactive, max {pool.maxPoolSize}{(pool.recycleActiveObjects ? ", recycles active" : "")}");
                }

                return builder.ToString();
            }

            builder.AppendLine("Configured pools:");

            foreach (Pool pool in settings.GetPoolsCopy())
            {
                string prefab = pool.prefab ? AssetDatabase.GetAssetPath(pool.prefab) : "no prefab";
                string preWarm = pool.preWarmPool ? $", prewarms {pool.preWarmPoolSize}" : "";

                builder.AppendLine($"  {pool.PoolName}: {prefab}, max {pool.maxPoolSize}{preWarm}{(pool.dontDestroyOnLoad ? ", persistent" : "")}");
            }

            return builder.ToString();
        }
    }
}
