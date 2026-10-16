using System;
using System.Text;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.AudioTrack;
using Unity.Pipeline.Commands;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.Systems.MCP
{
    internal static class AudioMcpCommands
    {
        [CliCommand("dn_list_audio_ids", "List the IDs registered in the Audio Library (for AudioLibrary.Play) and Audio Track (for AudioTrack.Play) settings")]
        private static string ListAudioIds(
            [CliArg("filter", "Only IDs containing this text (case-insensitive)")] string filter = "")
        {
            var builder = new StringBuilder();

            SOAudioLibrarySettings librarySettings = SOAudioLibrarySettings.Instance;

            if (!librarySettings)
            {
                builder.AppendLine("Audio Library: no AudioLibrarySettings asset in a Resources folder.");
            }
            else
            {
                builder.AppendLine($"Audio Library{(librarySettings.Enabled ? "" : " (disabled)")}:");

                foreach (SOAudioCategory category in librarySettings.AudioCategories)
                {
                    if (!category) continue;

                    builder.AppendLine($"  {category.label}{(category.AudioMixerGroup ? $" -> {category.AudioMixerGroup.name}" : "")}");

                    foreach (AudioMapping mapping in category.AudioMappings)
                    {
                        if (!Matches(mapping.id, filter)) continue;

                        builder.AppendLine($"    {mapping.id} ({DescribeAudioObject(mapping.audioObject)})");
                    }
                }
            }

            SOAudioTrackSettings trackSettings = SOAudioTrackSettings.Instance;

            if (!trackSettings)
            {
                builder.AppendLine("Audio Track: no AudioTrackSettings asset in a Resources folder.");
                return builder.ToString();
            }

            builder.AppendLine($"Audio Track{(trackSettings.Enabled ? "" : " (disabled)")}:");

            foreach (TrackDefinition track in trackSettings.Tracks)
            {
                if (!Matches(track.id, filter)) continue;

                builder.AppendLine($"  {track.id} ({(track.clip ? track.clip.name : "no clip")}, volume {track.volume:0.##})");
            }

            return builder.ToString();
        }

        private static bool Matches(string id, string filter)
        {
            return string.IsNullOrEmpty(filter) || (id != null && id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string DescribeAudioObject(Object audioObject)
        {
            return audioObject switch
            {
                SOAudioProfile profile => $"profile {profile.name}, {profile.clips?.Length ?? 0} clip(s){(profile.loop ? ", loops" : "")}",
                AudioClip clip => $"clip {clip.name}, {clip.length:0.##}s",
                _ when audioObject => audioObject.GetType().Name,
                _ => "missing"
            };
        }
    }
}
