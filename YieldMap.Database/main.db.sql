--SQLite Maestro 12.1.0.1
------------------------------------------
--Host     : localhost
--Database : C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Database\main.db


CREATE TABLE InstrumentBond (
  id              integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  id_Issuer       integer,
  id_Borrower     integer,
  id_Currency     integer,
  BondStructure   text,
  RateStructure   text,
  IssueSize       integer,
  Name            varchar(50) NOT NULL,
  IsCallable      bit,
  IsPutable       bit,
  Series          varchar(50),
  id_Isin         integer,
  id_Ric          integer,
  id_Ticker       integer,
  id_SubIndustry  integer,
  id_Type         integer,
  Issue           date,
  Maturity        date,
  /* Foreign keys */
  FOREIGN KEY (id_Currency)
    REFERENCES RefCurrency(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Borrower)
    REFERENCES RefBorrower(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Type)
    REFERENCES RefInstrument(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_SubIndustry)
    REFERENCES RefSubIndustry(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Ticker)
    REFERENCES RefTicker(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Ric)
    REFERENCES RefRic(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Isin)
    REFERENCES RefIsin(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (id_Issuer)
    REFERENCES RefIssuer(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX InstrumentBond_Index01
  ON InstrumentBond
  (id);

CREATE TABLE InstrumentCustomBond (
  id             integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name           varchar(50),
  BondStructure  text,
  RateStructure  text,
  id_Currency    integer,
  Issue          date,
  Maturity       date,
  /* Foreign keys */
  FOREIGN KEY (id_Currency)
    REFERENCES RefCurrency(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX InstrumentCustomBond_Index01
  ON InstrumentCustomBond
  (id);

CREATE TABLE RawBondInfo (
  id               integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  BondStructure    text,
  RateStructure    text,
  IssueSize        integer,
  IssuerName       varchar(50),
  BorrowerName     varchar(50),
  Coupon           float(50),
  Issue            date,
  Maturity         date,
  Currency         varchar(50),
  ShortName        varchar(50),
  IsCallable       bit,
  IsPutable        bit,
  IsFloater        bit,
  IsConvertible    bit,
  IsStraight       bit,
  Ticker           varchar(50),
  Series           varchar(50),
  BorrowerCountry  varchar(50),
  IssuerCountry    varchar(50),
  Isin             varchar(50),
  ParentTicker     varchar(50),
  Seniority        varchar(50),
  Industry         varchar(50),
  SubIndustry      varchar(50),
  Instrument       varchar(50)
);

CREATE INDEX RawBondInfo_Index01
  ON RawBondInfo
  (id);

CREATE TABLE RawFrnData (
  id       integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Cap      float(50),
  Floor    float(50),
  Margin   float(50),
  "Index"  varchar(50)
);

CREATE INDEX RawFrnData_Index01
  ON RawFrnData
  (id);

CREATE TABLE RawRating (
  id      integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  "Date"  date,
  Rating  varchar(50),
  Source  varchar(50),
  Issue   bit
);

CREATE INDEX RawRating_Index01
  ON RawRating
  (id);

CREATE TABLE RefBorrower (
  id          integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name        varchar(50) NOT NULL,
  id_Country  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (id_Country)
    REFERENCES RefCountry(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefBorrower_Index01
  ON RefBorrower
  (id);

CREATE INDEX RefBorrower_Index02
  ON RefBorrower
  (id);

CREATE TABLE RefChain (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE INDEX RefChain_Index01
  ON RefChain
  (id);

CREATE INDEX RefChain_Index02
  ON RefChain
  (Name);

CREATE TABLE RefCountry (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE INDEX RefCountry_Index01
  ON RefCountry
  (id);

CREATE INDEX RefCountry_Index02
  ON RefCountry
  (Name);

CREATE TABLE RefCurrency (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50)
);

CREATE INDEX RefCurrency_Index01
  ON RefCurrency
  (id);

CREATE TABLE RefIndustry (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) UNIQUE
);

CREATE INDEX RefIndustry_Index01
  ON RefIndustry
  (id);

CREATE INDEX RefIndustry_Index02
  ON RefIndustry
  (Name);

CREATE TABLE RefInstrument (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50)
);

CREATE INDEX RefInstrument_Index01
  ON RefInstrument
  (id);

CREATE TABLE RefIsin (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE INDEX RefIsin_Index01
  ON RefIsin
  (id);

CREATE INDEX RefIsin_Index02
  ON RefIsin
  (Name);

CREATE TABLE RefIssuer (
  id          integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name        varchar(50) NOT NULL,
  id_Country  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (id_Country)
    REFERENCES RefCountry(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefIssuer_Index01
  ON RefIssuer
  (id);

CREATE TABLE RefRating (
  id               integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Value            integer NOT NULL,
  Name             varchar(50) UNIQUE,
  id_RatingAgency  integer,
  /* Foreign keys */
  FOREIGN KEY (id)
    REFERENCES RefRatingAgency(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefRating_Index01
  ON RefRating
  (id);

CREATE INDEX RefRating_Index02
  ON RefRating
  (Name);

CREATE TABLE RefRatingAgency (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE INDEX RefRatingAgency_Index01
  ON RefRatingAgency
  (id);

CREATE INDEX RefRatingAgency_Index02
  ON RefRatingAgency
  (Name);

CREATE TABLE RefRic (
  id       integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name     varchar(50) NOT NULL,
  Isin_id  integer,
  /* Foreign keys */
  FOREIGN KEY (Isin_id)
    REFERENCES RefIsin(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefRic_Index01
  ON RefRic
  (id);

CREATE TABLE RefRicToChain (
  id        integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Ric_id    integer NOT NULL,
  Chain_id  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (Ric_id)
    REFERENCES RefRic(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (Chain_id)
    REFERENCES RefChain(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefRicToChain_Index01
  ON RefRicToChain
  (id);

CREATE TABLE RefSeniority (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE INDEX RefSeniority_Index01
  ON RefSeniority
  (id);

CREATE INDEX RefSeniority_Index02
  ON RefSeniority
  (Name);

CREATE TABLE RefSubIndustry (
  id           integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name         varchar(50) NOT NULL UNIQUE,
  id_Industry  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (id_Industry)
    REFERENCES RefIndustry(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefSubIndustry_Index01
  ON RefSubIndustry
  (id);

CREATE INDEX RefSubIndustry_Index02
  ON RefSubIndustry
  (Name);

CREATE TABLE RefTicker (
  id               integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name             varchar(50) NOT NULL UNIQUE,
  id_ParentTicker  integer,
  /* Foreign keys */
  FOREIGN KEY (id_ParentTicker)
    REFERENCES RefTicker(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE INDEX RefTicker_Index01
  ON RefTicker
  (id);

CREATE INDEX RefTicker_Index02
  ON RefTicker
  (Name);

