using System;
using System.Collections.Generic;
using System.Text;

namespace Fixer.HTTP.RestEase
{
    public interface IRestEaseServiceBuilder
    {
        IRestEaseServiceBuilder WithName(string name);
        IRestEaseServiceBuilder WithScheme(string scheme);
        IRestEaseServiceBuilder WithHost(string host);
        IRestEaseServiceBuilder WithPort(int port);
        RestEaseOptions.Service Build();
    }
}
