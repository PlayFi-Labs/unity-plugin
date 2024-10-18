

namespace PlayFi
{
    internal class ScreenshotWithData
    {
        internal ScreenshotWithData(byte[] image, string payload)
        {
            this.image = image;
            this.payload = payload;
        }
        
        private byte[] image;
        private string payload;

        internal byte[] Image
        {
            get => image;
            set => image = value;
        }

        internal string Payload
        {
            get => payload;
            set => payload = value;
        }
    }
}