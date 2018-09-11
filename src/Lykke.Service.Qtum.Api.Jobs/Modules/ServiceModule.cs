﻿using System;
using Autofac;
using Lykke.Service.Qtum.Api.AzureRepositories.Entities.Balances;
using Lykke.Service.Qtum.Api.AzureRepositories.Entities.Transactions;
using Lykke.Service.Qtum.Api.AzureRepositories.Repositories.Balances;
using Lykke.Service.Qtum.Api.AzureRepositories.Repositories.Transactions;
using Lykke.Service.Qtum.Api.Core.Helpers;
using Lykke.Service.Qtum.Api.Core.Repositories.Balances;
using Lykke.Service.Qtum.Api.Core.Repositories.Transactions;
using Lykke.Service.Qtum.Api.Core.Services;
using Lykke.Service.Qtum.Api.Jobs.PeriodicalHandlers;
using Lykke.Service.Qtum.Api.Jobs.Settings;
using Lykke.Service.Qtum.Api.Services;
using Lykke.SettingsReader;
using NBitcoin;
using NBitcoin.Qtum;


namespace Lykke.Service.Qtum.Api.Jobs.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Do not register entire settings in container, pass necessary settings to services which requires them
            
            // Network setup
            QtumNetworks.Register();
            builder.RegisterInstance(Network.GetNetwork(_appSettings.Nested(s => s.Network).CurrentValue)).As<Network>();
            
            // CoinConverter            
            builder.RegisterType<CoinConverter>()
                .As<CoinConverter>();
            
            // Repositories setup
            builder.RegisterType<BalanceObservationRepository>()
                .As<IBalanceObservationRepository<BalanceObservation>>()
                .WithParameter(TypedParameter.From(_appSettings.Nested(s => s.QtumApiService.Db.DataConnString)));
            
            builder.RegisterType<AddressBalanceRepository>()
                .As<IAddressBalanceRepository<AddressBalance>>()
                .WithParameter(TypedParameter.From(_appSettings.Nested(s => s.QtumApiService.Db.DataConnString)));
            
            builder.RegisterType<TransactionBodyRepository>()
                .As<ITransactionBodyRepository<TransactionBody>>()
                .WithParameter(TypedParameter.From(_appSettings.Nested(s => s.QtumApiJobsService.Db.DataConnString)));

            builder.RegisterType<TransactionMetaRepository>()
                .As<ITransactionMetaRepository<TransactionMeta>>()
                .WithParameter(TypedParameter.From(_appSettings.Nested(s => s.QtumApiJobsService.Db.DataConnString)));

            builder.RegisterType<TransactionObservationRepository>()
                .As<ITransactionObservationRepository<TransactionObservation>>()
                .WithParameter(TypedParameter.From(_appSettings.Nested(s => s.QtumApiJobsService.Db.DataConnString)));
            
            // Services setup
            builder.RegisterType<AssetService>()
                .As<IAssetService>()
                .SingleInstance();
            
            builder.RegisterType<BlockchainService>()
                .As<IBlockchainService>()
                .WithParameter("networkType", _appSettings.Nested(s => s.Network).CurrentValue);
            
            builder.RegisterType<BalanceService<BalanceObservation, AddressBalance>>()
                .As<IBalanceService<BalanceObservation, AddressBalance>>();
            
            builder.RegisterType<QtumInsightApi>()
                .As<IInsightApiService>()
                .WithParameter(TypedParameter.From(_appSettings.Nested(s => s.ExternalApi.QtumInsightApi).CurrentValue));
            
            builder.RegisterType<TransactionService<TransactionBody, TransactionMeta, TransactionObservation>>()
                .As<ITransactionService<TransactionBody, TransactionMeta, TransactionObservation>>();
                        
            //Jobs setup 
            builder.RegisterType<BalanceRefreshJob>()
                .As<IStartable>()
                .WithParameter(TypedParameter.From(TimeSpan.FromSeconds(10)))
                .SingleInstance();
            
            builder.RegisterType<BroadcastJob>()
                .As<IStartable>()
                .WithParameter(TypedParameter.From(TimeSpan.FromSeconds(10)))
                .SingleInstance();
        }
    }
}
