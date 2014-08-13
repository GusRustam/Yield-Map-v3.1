namespace YieldMap.Core

module DbManager = 
    open Autofac

    open YieldMap.Loader.SdkFactory
    open YieldMap.Loader.Calendar
    open YieldMap.Loader.MetaChains

    open System

    type Drivers = {
        TodayFix : DateTime
        Loader : ChainMetaLoader
        Factory : EikonFactory
        Calendar : Calendar
        DbServices : IContainer
    }