using System;
using Blackjack.Services.Transport;
using Reflex.Attributes;
using UnityEngine;

namespace Blackjack._Project.Scripts.Services.Auth
{
    public class NakamaBootstrap : MonoBehaviour
    {
        [Inject] NakamaTransport _nakamaTransport;

        void Start()
        {
            _nakamaTransport.Connect("defaultkey");
        }
    }
}