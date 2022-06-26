﻿using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Web.Commands.Models.Customers;
using MediatR;

namespace Grand.Web.Commands.Handler.Customers
{
    public class MagicLinkSendCommandHandler : IRequestHandler<MagicLinkSendCommand, bool>
    {
        private readonly IUserFieldService _userFieldService;
        private readonly IMessageProviderService _messageProviderService;

        public MagicLinkSendCommandHandler(
            IUserFieldService userFieldService,
            IMessageProviderService messageProviderService)
        {
            _userFieldService = userFieldService;
            _messageProviderService = messageProviderService;
        }

        public async Task<bool> Handle(MagicLinkSendCommand request, CancellationToken cancellationToken)
        {
    
            // Generate GUID & Timestamp
            var loginCode = (Guid.NewGuid()).ToString();
            long loginCodeExpiry = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds() + (request.MinutesToExpire * 60);


            // Encrypt loginCode
            var salt = request.Customer.PasswordSalt;
            var hashedLoginCode = request.EncryptionService.CreatePasswordHash(loginCode, salt, request.HashedPasswordFormat);

            // Save to Db
            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.EmailLoginToken, hashedLoginCode);
            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.EmailLoginTokenExpiry, loginCodeExpiry);
           
            // Send email
            await _messageProviderService.SendCustomerEmailLoginLinkMessage(request.Customer, request.Store, request.Language.Id, loginCode);

            return true;
        }
    }
}