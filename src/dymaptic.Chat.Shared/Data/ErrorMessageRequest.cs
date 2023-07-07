using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dymaptic.Chat.Shared.Data
{
    public record ErrorMessageRequest(Guid ErrorToken, string? ExceptionMessage, string? ExceptionStackTrack, string? ExceptionInnerException)
    {
    
    }

}
