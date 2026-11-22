namespace ChessDroid.Services
{
    public class AudioService
    {
        private System.Media.SoundPlayer? _sndMove;
        private System.Media.SoundPlayer? _sndCapture;
        private System.Media.SoundPlayer? _sndCheck;
        private System.Media.SoundPlayer? _sndGameOver;
        private bool _gameOverSoundPlayed;

        public void Initialize(string audioDir)
        {
            TryLoadSound(ref _sndMove,     Path.Combine(audioDir, "piece_move.wav"),  gainFactor: 1.5f);
            TryLoadSound(ref _sndCapture,  Path.Combine(audioDir, "piece_take.wav"),  gainFactor: 1.0f);
            TryLoadSound(ref _sndCheck,    Path.Combine(audioDir, "piece_check.wav"), gainFactor: 1.0f);
            TryLoadSound(ref _sndGameOver, Path.Combine(audioDir, "game_over.wav"),   gainFactor: 1.0f);
        }

        public void PlayMoveSound(bool isCapture, string san, bool soundEnabled)
        {
            if (!soundEnabled) return;
            try
            {
                if (san.EndsWith('#'))
                {
                    _gameOverSoundPlayed = true;
                    _sndGameOver?.Play();
                }
                else if (san.EndsWith('+'))    _sndCheck?.Play();
                else if (san.StartsWith("O-O"))
                {
                    _sndMove?.Play();
                    Task.Delay(70).ContinueWith(_ => _sndMove?.Play());
                }
                else if (isCapture)            _sndCapture?.Play();
                else                           _sndMove?.Play();
            }
            catch { }
        }

        public void PlayGameEndSound(bool soundEnabled)
        {
            if (!soundEnabled) return;
            if (_gameOverSoundPlayed) { _gameOverSoundPlayed = false; return; }
            try { _sndGameOver?.Play(); }
            catch { }
        }

        private static void TryLoadSound(ref System.Media.SoundPlayer? player, string path, float gainFactor = 1.0f)
        {
            if (!File.Exists(path)) return;
            try
            {
                byte[] data = File.ReadAllBytes(path);
                if (gainFactor != 1.0f) AmplifyWav(data, gainFactor);
                player = new System.Media.SoundPlayer(new MemoryStream(data));
                player.Load();
            }
            catch { player = null; }
        }

        // Amplifies 16-bit PCM WAV samples in-place. Silently no-ops for non-16-bit files.
        private static void AmplifyWav(byte[] wav, float gain)
        {
            if (wav.Length < 44) return;
            short bitsPerSample = 16;
            int dataStart = -1, dataSize = 0;
            int i = 12;
            while (i < wav.Length - 8)
            {
                string id = System.Text.Encoding.ASCII.GetString(wav, i, 4);
                int size = BitConverter.ToInt32(wav, i + 4);
                if (id == "fmt " && size >= 16)
                    bitsPerSample = BitConverter.ToInt16(wav, i + 22);
                else if (id == "data") { dataStart = i + 8; dataSize = size; break; }
                i += 8 + size + (size % 2);
            }
            if (dataStart < 0 || bitsPerSample != 16) return;
            int dataEnd = Math.Min(dataStart + dataSize, wav.Length);
            for (int j = dataStart; j < dataEnd - 1; j += 2)
            {
                short sample = BitConverter.ToInt16(wav, j);
                short boosted = (short)Math.Clamp(sample * gain, short.MinValue, short.MaxValue);
                wav[j]     = (byte)(boosted & 0xFF);
                wav[j + 1] = (byte)((boosted >> 8) & 0xFF);
            }
        }
    }
}
