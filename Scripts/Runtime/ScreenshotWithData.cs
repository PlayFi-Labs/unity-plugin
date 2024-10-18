

namespace PlayFi
{
    internal class ScreenshotWithData
    {
        internal ScreenshotWithData(byte[] image, string modelId, string payload)
        {
            this.image = image;
            this.ModelId = modelId;
            this.payload = payload;
        }
        
        private byte[] image;
        private string modelId;
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

        public string ModelId
        {
            get => modelId;
            set => modelId = value;
        }
    }
}