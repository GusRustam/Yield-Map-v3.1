--SQLite Maestro 12.1.0.1
------------------------------------------
--Host     : localhost
--Database : C:\Users\Rustam Guseynov\Documents\yieldmap.sqlite3


CREATE TABLE InstrumentBond (
  id              integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  id_Issuer       integer,
  id_Borrower     integer,
  id_Currency     integer,
  BondStructure   varchar(50),
  RateStructure   varchar(50),
  IssueSize       integer,
  ShortName       varchar(50) NOT NULL,
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

CREATE TABLE RawFrnData (
  id       integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Cap      float(50),
  Floor    float(50),
  Margin   float(50),
  "Index"  varchar(50)
);

CREATE TABLE RawRating (
  id      integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  "Date"  date,
  Rating  varchar(50),
  Source  varchar(50),
  Issue   bit
);

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

CREATE TABLE RefChain (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE TABLE RefCountry (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE TABLE RefCurrency (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50)
);

CREATE TABLE RefIndustry (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) UNIQUE
);

CREATE TABLE RefInstrument (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50)
);

CREATE TABLE RefIsin (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

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

CREATE TABLE RefRatingAgency (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

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

CREATE TABLE RefRicToChain (
  id        integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Ric_id    integer NOT NULL,
  Chain_id  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (Chain_id)
    REFERENCES RefChain(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT, 
  FOREIGN KEY (Ric_id)
    REFERENCES RefRic(id)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
);

CREATE TABLE RefSeniority (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

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

/* Data for table InstrumentBond */




/* Data for table InstrumentCustomBond */




/* Data for table RawBondInfo */




/* Data for table RawFrnData */




/* Data for table RawRating */




/* Data for table RefBorrower */




/* Data for table RefChain */




/* Data for table RefCountry */




/* Data for table RefCurrency */




/* Data for table RefIndustry */




/* Data for table RefInstrument */




/* Data for table RefIsin */




/* Data for table RefIssuer */




/* Data for table RefRating */




/* Data for table RefRatingAgency */




/* Data for table RefRic */




/* Data for table RefRicToChain */




/* Data for table RefSeniority */




/* Data for table RefSubIndustry */




/* Data for table RefTicker */


