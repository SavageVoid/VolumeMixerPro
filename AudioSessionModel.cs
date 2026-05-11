using System.Windows.Media;
namespace VolumeMixerPro
{
    public class AudioSessionModel
    {
        public int         ProcessId   { get; set; }
        public string      ProcessName { get; set; }  
        public string      DisplayName { get; set; }  
        public float       Volume      { get; set; }  
        public bool        IsMuted     { get; set; }
        public ImageSource AppIcon     { get; set; }
    }
}
