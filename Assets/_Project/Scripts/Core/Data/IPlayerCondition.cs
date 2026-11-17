using System;

namespace Zmywak.Core.Data
{
    /// <summary>
    /// How Kuba is when he walks into the kitchen. Written by the life between shifts (MetaProgressController
    /// registers itself as this), read by the kitchen - so neither module has to know the other.
    /// </summary>
    public interface IPlayerCondition
    {
        /// <summary>0-100.</summary>
        float Energy { get; }

        /// <summary>The date in the game - the kitchen pays the wage of the day.</summary>
        DateTime Today { get; }

        /// <summary>Where Kuba lives: what the kitchen pays, and in what money.</summary>
        Place Place { get; }

        /// <summary>His rank at the sink (<see cref="Career"/>): each one adds to the hourly wage.</summary>
        int Rank { get; }

        /// <summary>How much one of the crew he is, 0-100: on a hard night, a friend at the next sink lends a hand.</summary>
        float Integration { get; }

        /// <summary>His whole record at the sink so far (the result screen shows the next rank and the records).</summary>
        CareerRecord WorkRecord { get; }
    }
}
