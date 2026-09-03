namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// The two-roll wager behind <see cref="SubActionType.GambleRoll"/> — <c>PerformSubAction</c>
/// case 8, @0x4100a.
/// </summary>
/// <remarks>
/// <b>Both sides roll a die and the higher number takes the money.</b> The party rolls
/// <c>Field2</c> sides, the house rolls <c>Field4</c>, and on a win the party is paid
/// <c>Field6</c> percent of the quoted price. What moves besides the purse is the
/// establishment's own fund: a win takes the payout OUT of it, a loss puts the stake IN.
///
/// <para><b>*** A DRAW DOES NOTHING, AND THAT IS FROM THE DISASSEMBLY, NOT FROM THE C. ***</b>
/// canassa renders the tail as <c>else if (a &lt; b)</c> after <c>else if (a &lt; b)</c> — the same
/// condition twice, so the third arm cannot run as written, and it was not safe to assume that
/// was a decompilation artifact. It is not one. At 0x410c8 the binary compares the two globals a
/// third time and branches on <c>jl</c>, the same direction as the second arm's <c>jge</c> at
/// 0x4109b — so reaching the third test means the two are EQUAL, and <c>jl</c> is false. Control
/// falls straight to the return.
///
/// <para>So on a draw the outcome global is left holding whatever the previous wager put there,
/// the purse does not move and the fund does not move. <see cref="Settle"/> reports that as
/// <see cref="Result.Settled"/> being false rather than as a third outcome value, because writing
/// "2" would be inventing a state the shipped game never reaches.</para></para>
/// </remarks>
public static class DialogWager {
    /// <summary>Outcome global value for a win.</summary>
    public const int PartyWins = 0;

    /// <summary>Outcome global value for a loss.</summary>
    public const int PartyLoses = 1;

    /// <summary>
    /// The fund stops growing here — <c>cmp global_reward?_money?, 60000</c> at 0x410ac.
    /// </summary>
    /// <remarks>
    /// A ceiling on the ADD, not a clamp on the result: past it a loss simply adds nothing, so the
    /// fund can sit above the line only if something else put it there.
    /// </remarks>
    public const int FundCeiling = 60000;

    /// <summary>
    /// One die, the original's way: a 12-bit draw taken modulo the number of sides.
    /// </summary>
    /// <remarks>
    /// <c>GetRandomNumber()</c>, masked to <c>0xFFF</c>, then an unsigned <c>div</c> whose
    /// REMAINDER is kept. Zero sides would trap the original's <c>div</c>; here it answers 0, since
    /// a dialog authored with a zero die should not take the game down.
    /// </remarks>
    public static int RollDie(int twelveBitRoll, int sides) =>
        sides <= 0 ? 0 : twelveBitRoll % sides;

    /// <summary>What a wager did.</summary>
    public readonly struct Result {
        public Result(bool settled, int outcome, int goldDelta, int fund) {
            Settled = settled;
            Outcome = outcome;
            GoldDelta = goldDelta;
            Fund = fund;
        }

        /// <summary>False on a draw — nothing moved and no outcome was written.</summary>
        public bool Settled { get; }

        /// <summary><see cref="PartyWins"/> or <see cref="PartyLoses"/>; meaningless when not
        /// <see cref="Settled"/>.</summary>
        public int Outcome { get; }

        /// <summary>Signed change to the party's purse.</summary>
        public int GoldDelta { get; }

        /// <summary>The establishment's fund afterwards.</summary>
        public int Fund { get; }
    }

    /// <summary>
    /// Settles one wager.
    /// </summary>
    /// <param name="partyRoll">The party's die, already rolled.</param>
    /// <param name="houseRoll">The house's die, already rolled.</param>
    /// <param name="quotedPrice">The stake — global 30014.</param>
    /// <param name="winPercent">Percent of the stake a win pays — <c>Field6</c>.</param>
    /// <param name="fund">The establishment's fund before the wager.</param>
    /// <remarks>
    /// <b>The payout is truncated to a SIGNED 16-BIT value on the way through.</b> The original
    /// computes it in 32 bits, stores the low word to a stack int and sign-extends that back out
    /// (0x41064, 0x4106d), so a payout past 32767 would come back negative and pay the house. No
    /// shipped stake reaches it — this is recorded because the arithmetic is the original's shape,
    /// not because it bites.
    ///
    /// <para>A win takes the payout off the fund, FLOORED at zero, so a house that cannot cover
    /// the win still pays the party in full out of nowhere; a loss adds the stake unless the fund
    /// is already at <see cref="FundCeiling"/>.</para>
    /// </remarks>
    public static Result Settle(int partyRoll, int houseRoll, int quotedPrice, int winPercent,
        int fund) {
        if (partyRoll > houseRoll) {
            var payout = (short)(quotedPrice * winPercent / 100);

            return new Result(true, PartyWins, payout, fund < payout ? 0 : fund - payout);
        }

        if (partyRoll < houseRoll) {
            return new Result(true, PartyLoses, -quotedPrice,
                fund < FundCeiling ? fund + quotedPrice : fund);
        }

        return new Result(false, 0, 0, fund);
    }
}
