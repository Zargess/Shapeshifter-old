using System.Linq;
using System.Threading.Tasks;
using Shapeshifter.Desktop.Functionality.Clipboard.Data;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;
using Shapeshifter.Desktop.Functionality.Factories;

namespace Shapeshifter.Desktop.Functionality.Clipboard.Factories
{
    public sealed class ClipboardCustomDataFactory : ClipboardItemFactory
    {
        public ClipboardCustomDataFactory(ClipboardSnapshot snapshot = null) : base(snapshot)
        {
        }
        
#if NETFX_CORE
        protected async override Task<ClipboardItem> CreateNew(ClipboardSnapshot snapshot)
#else
        protected override ClipboardItem CreateNew(ClipboardSnapshot snapshot)
#endif
        {
            var preferredFormat = snapshot.FormatPriorities.First();

            var result = new ClipboardCustomData();

#if !NETFX_CORE
            result.Name = snapshot.GetClipboardFormatName(preferredFormat);
            result.Icon = ClipboardSource.IconFromWindowHandle(GetClipboardOwner(), 64);
#else
            result.Name = preferredFormat;
            //TODO: shapeshifter icon or something else which symbolizes custom data
#endif

            return result;
           
        }
    }
}