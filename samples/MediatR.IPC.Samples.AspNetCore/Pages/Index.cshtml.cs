#if MEDIATR
using MediatR.IPC.Samples.AssemblyScan;
#else
using Mediator.IPC.Samples.AssemblyScan;
#endif
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
.Samples.AspNetCore.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ISender sender;

        public IndexModel(ILogger<IndexModel> logger, ISender sender)
        {
            _logger = logger;
            this.sender = sender;
        }

        [BindProperty]
        public string Message { get; set; }

        public void OnGet()
        {

        }

        public async Task OnPost()
        {
            Console.WriteLine("Sending message..");
            var pid = Process.GetCurrentProcess().Id;
            var response = await sender.Send(new IPCMessageCommand { PID = pid, Message = Message });
            Console.WriteLine($"Got response: {response}");
        }
    }
}
