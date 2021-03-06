﻿using Client.Messages;
using Common.Abstractions.Interfaces;
using Common.Enums;
using Common.Messages;
using Common.Models;
using Newtonsoft.Json;
using Prism.Events;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace Client.Handlers
{
    public class ClientTcpHandler : ITcpHandler
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private readonly IEventAggregator _eventAggregator;

        public BinaryReader Reader { get; set; }
        public BinaryWriter Writer { get; set; }

        public ClientTcpHandler(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            SubscribeEvents();
        }

        public void Start()
        {
            _tcpClient = new TcpClient("localhost", 8888);

            _eventAggregator.GetEvent<AddConsoleMessage>().Publish("Conenction estabilished with server..\n\n");

            _networkStream = _tcpClient.GetStream();
            Reader = new BinaryReader(_networkStream);
            Writer = new BinaryWriter(_networkStream);
        }

        public void CloseConnection()
        {
            _tcpClient.Close();
            _networkStream.Close();
            Reader.Close();
            Writer.Close();
        }

        private void SubscribeEvents()
        {
            _eventAggregator.GetEvent<GetAllContactsMessage>().Subscribe(SendServerGetAllContactsMessage);
            _eventAggregator.GetEvent<SearchContactByNameMessage>().Subscribe((searchText) => SendServerSearchContacts(ActionType.SearchContactByName, searchText));
            _eventAggregator.GetEvent<SearchContactByTelephoneMessage>().Subscribe((searchText) => SendServerSearchContacts(ActionType.SearchContactByTelephone, searchText));
            _eventAggregator.GetEvent<CreateContactMessage>().Subscribe((contact) => SendServerMessage(ActionType.NewContact, contact));
            _eventAggregator.GetEvent<EditContactMessage>().Subscribe((contact) => SendServerMessage(ActionType.EditContact, contact));
            _eventAggregator.GetEvent<DeleteContactMessage>().Subscribe((id) => SendServerMessage(ActionType.DeleteContact, id));
        }

        private void SendServerMessage(ActionType actionType, object content)
        {
            var jsonContent = JsonConvert.SerializeObject(content);
            var message = new MessageExchange(actionType, jsonContent);
            var jsonMessage = JsonConvert.SerializeObject(message);

            Writer.Write(jsonMessage);

            _eventAggregator.GetEvent<AddConsoleMessage>().Publish("Waiting server response..\n\n");

            var messageFromServer = Reader.ReadString();

            _eventAggregator.GetEvent<AddConsoleMessage>().Publish(messageFromServer);
        }

        private void SendServerSearchContacts(ActionType actionType, string searchText)
        {
            var message = new MessageExchange(actionType);
            message.Content = searchText;
            var jsonMessage = JsonConvert.SerializeObject(message);

            Writer.Write(jsonMessage);

            _eventAggregator.GetEvent<AddConsoleMessage>().Publish("Waiting server response..\n\n");

            var result = Reader.ReadString();
            var contacts = JsonConvert.DeserializeObject<List<Contact>>(result);

            _eventAggregator.GetEvent<UpdateContactsMessage>().Publish(contacts);

            var messageFromServer = Reader.ReadString();

            _eventAggregator.GetEvent<AddConsoleMessage>().Publish(messageFromServer);
        }

        private void SendServerGetAllContactsMessage()
        {
            var message = new MessageExchange(ActionType.GetAllContacts);
            var jsonMessage = JsonConvert.SerializeObject(message);

            Writer.Write(jsonMessage);

            _eventAggregator.GetEvent<AddConsoleMessage>().Publish("Waiting server response..\n\n");

            var result = Reader.ReadString();
            var contacts = JsonConvert.DeserializeObject<List<Contact>>(result);

            _eventAggregator.GetEvent<UpdateContactsMessage>().Publish(contacts);

            var messageFromServer = Reader.ReadString();

            _eventAggregator.GetEvent<AddConsoleMessage>().Publish(messageFromServer);
        }
    }
}