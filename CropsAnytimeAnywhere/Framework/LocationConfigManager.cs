using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace Pathoschild.Stardew.CropsAnytimeAnywhere.Framework
{
    /// <summary>Encapsulates access to the per-location configuration.</summary>
    internal class LocationConfigManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>A lookup cache of configurations by location key.</summary>
        private readonly IDictionary<string, PerLocationConfig> ConfigCache = new Dictionary<string, PerLocationConfig>();

        /// <summary>The underlying mod configuration.</summary>
        private readonly ModConfig Config;

        /// <summary>Whether there's only one location config defined and it's for the <c>*</c> key.</summary>
        private readonly bool OnlyHasGlobal;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="config">The underlying mod configuration.</param>
        public LocationConfigManager(ModConfig config)
        {
            this.Config = config;
            this.OnlyHasGlobal = config.InLocations?.Count == 1 && config.InLocations.Keys.Single() == "*";
        }

        /// <summary>Whether any of the locations override tile tillability.</summary>
        public bool HasTillableOverrides()
        {
            return this.Config.ForceTillable?.IsAnyEnabled() == true;
        }

        /// <summary>Get the tiles which should be forced tillable, if any.</summary>
        public ModConfigForceTillable GetForceTillableConfig()
        {
            return this.Config.ForceTillable;
        }

        /// <summary>Get the location config that applies for a given location name.</summary>
        /// <param name="location">The location.</param>
        public PerLocationConfig GetForLocation(GameLocation location)
        {
            // shortcut for common cases
            if (this.Config.InLocations is null)
                return null;
            if (this.OnlyHasGlobal)
                return this.Config.InLocations["*"];

            // get config with caching
            string cacheKey = $"{location.NameOrUniqueName}|{location.IsOutdoors}|{location.GetHashCode()}";
            if (!this.ConfigCache.TryGetValue(cacheKey, out PerLocationConfig config))
            {
                this.ConfigCache[cacheKey] = config =
                    (
                        from entry in this.Config.InLocations
                        where this.AppliesTo(entry.Key, location)
                        select entry.Value
                    )
                    .LastOrDefault();
            }

            return config;
        }

        /// <summary>Get the configuration that applies for a given location, if any.</summary>
        /// <param name="location">The location being patched.</param>
        /// <param name="config">The config to apply, if any.</param>
        public bool TryGetForLocation(GameLocation location, out PerLocationConfig config)
        {
            config = this.GetForLocation(location);
            return config != null;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether this config applies to the given location.</summary>
        /// <param name="key">The per-location key.</param>
        /// <param name="location">The location instance.</param>
        private bool AppliesTo(string key, GameLocation location)
        {
            key = key.ToLower();
            string name = location.Name?.ToLower();
            string uniqueName = location.NameOrUniqueName?.ToLower();

            switch (key)
            {
                case "*":
                    return true;

                case "indoor":
                case "indoors":
                    return !location.IsOutdoors;

                case "outdoor":
                case "outdoors":
                    return location.IsOutdoors;

                default:
                    return key == name || key == uniqueName;
            }
        }
    }
}
