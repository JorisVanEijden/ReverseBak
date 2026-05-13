namespace GameData.Resources.Audio;

// CHAPSONG.DAT — 36-byte table mapping each chapter book to its background song.
//
// The DOS function `open_book?` at seg020:0x20d21 reads a single i16 from offset
//   ((chapter - 1) * 2 + (book - 1)) * 2
// for book ∈ {1, 2}, then passes the value to `audio_song_sub_1505A` before
// loading and displaying C{chapter}{book}.BOK. A song value of -999 is a sentinel
// that means "leave the current song playing" — see `audio_song_sub_1505A` at
// 0x15072 which short-circuits and returns the currently-playing song id.
public class ChapterSongMap : IResource {
    public const short NoChange = -999;

    public ChapterSongMap(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    // Indexed [0..8] for chapters 1..9. Each entry holds the two slots
    // for that chapter's intro (C{n}1.BOK) and mid-chapter (C{n}2.BOK) books.
    public List<ChapterSongEntry> Entries { get; set; } = [];
}

public class ChapterSongEntry {
    public short Book1Song { get; set; }
    public short Book2Song { get; set; }
}
