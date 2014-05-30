--SQLite Maestro 12.1.0.1
------------------------------------------
--Host     : localhost
--Database : C:\Users\Rustam Guseynov\Documents\Visual Studio 2012\Projects\Yield Map v3.1\YieldMap.Database\main.db


CREATE TABLE Chain (
  id        integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name      varchar(50) NOT NULL UNIQUE,
  id_Feed   integer,
  Expanded  date,
  Params    varchar(50) NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (id_Feed)
    REFERENCES Feed(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE INDEX RefChain_Index01
  ON Chain
  (id);

CREATE INDEX RefChain_Index02
  ON Chain
  (Name);

CREATE TABLE Country (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE INDEX RefCountry_Index01
  ON Country
  (id);

CREATE INDEX RefCountry_Index02
  ON Country
  (Name);

CREATE TABLE Currency (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50)
);

CREATE INDEX RefCurrency_Index01
  ON Currency
  (id);

CREATE TABLE Description (
  id              integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  id_Issuer       integer,
  id_Borrower     integer,
  RateStructure   text,
  IssueSize       integer,
  Series          varchar(50),
  id_Isin         integer,
  id_Ric          integer,
  id_Ticker       integer,
  id_SubIndustry  integer,
  id_Specimen     integer,
  Issue           date,
  Maturity        date,
  id_Seniority    integer,
  NextCoupon      date,
  /* Foreign keys */
  FOREIGN KEY (id_Borrower)
    REFERENCES LegalEntity(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Specimen)
    REFERENCES Specimen(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_SubIndustry)
    REFERENCES SubIndustry(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Ticker)
    REFERENCES Ticker(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Ric)
    REFERENCES Ric(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Isin)
    REFERENCES Isin(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Issuer)
    REFERENCES LegalEntity(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Seniority)
    REFERENCES Seniority(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE TABLE Feed (
  id           integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name         varchar(50),
  Description  varchar(50)
);

CREATE TABLE Field (
  id             integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Bid            varchar(50),
  Ask            varchar(50),
  Last           varchar(50),
  Close          varchar(50),
  VWAP           varchar(50),
  Volume         varchar(50),
  id_FieldGroup  integer NOT NULL,
  "Default"      boolean NOT NULL DEFAULT false,
  /* Foreign keys */
  FOREIGN KEY (id_FieldGroup)
    REFERENCES FieldGroup(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE TRIGGER Field_AfterUpdate
  AFTER UPDATE
  ON Field
  WHEN NEW."Default" = 1 AND EXISTS (SELECT SUM("Default") AS x FROM Field GROUP BY "Default" HAVING x >= 1)
BEGIN
  --INSERT INTO _log(msg) VALUES("Setting new default");
  UPDATE Field SET "Default" = 0 WHERE Field.id <> NEW.id;
END;

CREATE TRIGGER Field_BeforeUpdate
  BEFORE UPDATE
  ON Field
  WHEN NEW."Default" = 0 AND NOT EXISTS (SELECT SUM("Default") AS x FROM Field GROUP BY "Default" HAVING x > 1)
BEGIN
  --INSERT INTO _log(msg) VALUES("Trying to remove last default");
  SELECT RAISE(IGNORE);
END;

CREATE TABLE FieldGroup (
  id            integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name          varchar(50) NOT NULL UNIQUE,
  DefaultField  varchar(50),
  "Default"     boolean NOT NULL DEFAULT false
);

CREATE TRIGGER FieldGroup_AfterUpdate
  AFTER UPDATE
  ON FieldGroup
  WHEN NEW."Default" = 1 AND EXISTS (SELECT SUM("Default") AS x FROM FieldGroup GROUP BY "Default" HAVING x >= 1)
BEGIN
  --INSERT INTO _log(msg) VALUES("Setting new default");
  UPDATE FieldGroup SET "Default" = 0 WHERE FieldGroup.id <> NEW.id;
END;

CREATE TRIGGER FieldGroup_BeforeUpdate
  BEFORE UPDATE
  ON FieldGroup
  WHEN NEW."Default" = 0 AND NOT EXISTS (SELECT SUM("Default") AS x FROM FieldGroup GROUP BY "Default" HAVING x > 1)
BEGIN
  --INSERT INTO _log(msg) VALUES("Trying to remove last default");
  SELECT RAISE(IGNORE);
END;

CREATE TABLE "Index" (
  id      integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name    varchar(50) NOT NULL UNIQUE,
  id_Ric  integer,
  /* Foreign keys */
  FOREIGN KEY (id_Ric)
    REFERENCES Ric(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE INDEX Index_Index01
  ON "Index"
  (id);

CREATE INDEX Index_Index02
  ON "Index"
  (Name);

CREATE TABLE Industry (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) UNIQUE
);

CREATE INDEX RefIndustry_Index01
  ON Industry
  (id);

CREATE INDEX RefIndustry_Index02
  ON Industry
  (Name);

CREATE TABLE Instrument (
  id                 integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name               varchar(50),
  id_InstrumentType  integer,
  id_Description     integer,
  /* Foreign keys */
  FOREIGN KEY (id_Description)
    REFERENCES Description(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_InstrumentType)
    REFERENCES InstrumentType(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE TABLE InstrumentType (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE TABLE Isin (
  id       integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name     varchar(50) NOT NULL UNIQUE,
  id_Feed  integer,
  /* Foreign keys */
  FOREIGN KEY (id_Feed)
    REFERENCES Feed(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE INDEX RefIsin_Index01
  ON Isin
  (id);

CREATE INDEX RefIsin_Index02
  ON Isin
  (Name);

CREATE TABLE Leg (
  id             integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Structure      varchar(1000),
  id_Instrument  integer,
  id_LegType     integer,
  id_Currency    integer,
  FixedRate      float(50),
  id_Index       integer,
  Cap            float(50),
  Floor          float(50),
  Margin         float(50),
  /* Foreign keys */
  FOREIGN KEY (id_Index)
    REFERENCES "Index"(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_LegType)
    REFERENCES LegType(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Instrument)
    REFERENCES Instrument(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Currency)
    REFERENCES Currency(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE UNIQUE INDEX Leg_Index01
  ON Leg
  (id, id_Instrument, id_LegType);

CREATE TABLE LegType (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) UNIQUE
);

CREATE TABLE LegalEntity (
  id          integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name        varchar(255) NOT NULL,
  id_Country  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (id_Country)
    REFERENCES Country(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE INDEX Borrower01_Index01
  ON LegalEntity
  (id);

CREATE INDEX Borrower01_Index02
  ON LegalEntity
  (id);

CREATE INDEX Borrower01_Index03
  ON LegalEntity
  (id);

CREATE TABLE Rating (
  id               integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Value            integer NOT NULL,
  Name             varchar(50) NOT NULL,
  id_RatingAgency  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (id_RatingAgency)
    REFERENCES RatingAgency(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE INDEX RefRating_Index01
  ON Rating
  (id);

CREATE INDEX RefRating_Index02
  ON Rating
  (Name);

CREATE TABLE RatingAgency (
  id           integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name         varchar(50) NOT NULL UNIQUE,
  Description  varchar(50)
);

CREATE INDEX RefRatingAgency_Index01
  ON RatingAgency
  (id);

CREATE INDEX RefRatingAgency_Index02
  ON RatingAgency
  (Name);

CREATE TABLE RatingAgencyCode (
  id               integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name             varchar(50) UNIQUE,
  id_RatingAgency  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (id_RatingAgency)
    REFERENCES RatingAgency(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE TABLE RatingToInstrument (
  id             integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  id_Rating      integer NOT NULL,
  id_Instrument  integer,
  /* Foreign keys */
  FOREIGN KEY (id_Rating)
    REFERENCES Rating(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Instrument)
    REFERENCES Instrument(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE TABLE RatingToLegalEntity (
  id              integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  id_Rating       integer NOT NULL,
  id_LegalEntity  integer,
  /* Foreign keys */
  FOREIGN KEY (id_LegalEntity)
    REFERENCES LegalEntity(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Rating)
    REFERENCES Rating(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE TABLE Ric (
  id             integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name           varchar(50) NOT NULL,
  id_Isin        integer,
  id_Feed        integer,
  id_FieldGroup  integer,
  /* Foreign keys */
  FOREIGN KEY (id_FieldGroup)
    REFERENCES FieldGroup(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Isin)
    REFERENCES Isin(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (id_Feed)
    REFERENCES Feed(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE INDEX RefRic_Index01
  ON Ric
  (id);

CREATE TABLE RicToChain (
  id        integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Ric_id    integer,
  Chain_id  integer,
  /* Foreign keys */
  FOREIGN KEY (Chain_id)
    REFERENCES Chain(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE, 
  FOREIGN KEY (Ric_id)
    REFERENCES Ric(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE UNIQUE INDEX RicToChain_Index01
  ON RicToChain
  (Chain_id, Ric_id);

CREATE TABLE Seniority (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name  varchar(50) NOT NULL UNIQUE
);

CREATE INDEX RefSeniority_Index01
  ON Seniority
  (id);

CREATE INDEX RefSeniority_Index02
  ON Seniority
  (Name);

CREATE TABLE Specimen (
  id    integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  name  varchar(50) NOT NULL
);

CREATE TABLE SubIndustry (
  id           integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name         varchar(50) NOT NULL UNIQUE,
  id_Industry  integer NOT NULL,
  /* Foreign keys */
  FOREIGN KEY (id_Industry)
    REFERENCES Industry(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE INDEX RefSubIndustry_Index01
  ON SubIndustry
  (id);

CREATE INDEX RefSubIndustry_Index02
  ON SubIndustry
  (Name);

CREATE TABLE Ticker (
  id               integer PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
  Name             varchar(50) NOT NULL UNIQUE,
  id_ParentTicker  integer,
  /* Foreign keys */
  FOREIGN KEY (id_ParentTicker)
    REFERENCES Ticker(id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
);

CREATE INDEX RefTicker_Index01
  ON Ticker
  (id);

CREATE INDEX RefTicker_Index02
  ON Ticker
  (Name);

CREATE TABLE _log (
  msg  varchar(255)
);

/* Data for table Chain */




/* Data for table Country */




/* Data for table Currency */




/* Data for table Description */




/* Data for table Feed */




/* Data for table Field */
INSERT INTO Field (id, Bid, Ask, Last, Close, VWAP, Volume, id_FieldGroup, "Default") VALUES (1, 'BID', 'ASK', 'LAST', 'CLOSE', 'VWAP', 'VOLUME', 1, 1);
INSERT INTO Field (id, Bid, Ask, Last, Close, VWAP, Volume, id_FieldGroup, "Default") VALUES (2, '393', '275', NULL, NULL, NULL, NULL, 2, 0);
INSERT INTO Field (id, Bid, Ask, Last, Close, VWAP, Volume, id_FieldGroup, "Default") VALUES (3, NULL, NULL, '1054', NULL, NULL, NULL, 3, 0);



/* Data for table FieldGroup */
INSERT INTO FieldGroup (id, Name, DefaultField, "Default") VALUES (1, 'Micex', 'Last', 0);
INSERT INTO FieldGroup (id, Name, DefaultField, "Default") VALUES (2, 'Eurobonds', 'Bid', 1);
INSERT INTO FieldGroup (id, Name, DefaultField, "Default") VALUES (3, 'Russian CPI Index', 'Last', 0);



/* Data for table Index */
INSERT INTO "Index" (id, Name, id_Ric) VALUES (1, 'RUCPI1M', NULL);
INSERT INTO "Index" (id, Name, id_Ric) VALUES (2, 'RUSSRR', NULL);
INSERT INTO "Index" (id, Name, id_Ric) VALUES (3, 'MPRIME3M', NULL);



/* Data for table Industry */




/* Data for table Instrument */




/* Data for table InstrumentType */
INSERT INTO InstrumentType (id, Name) VALUES (1, 'Bond');
INSERT INTO InstrumentType (id, Name) VALUES (2, 'Frn');
INSERT INTO InstrumentType (id, Name) VALUES (3, 'Swap');
INSERT INTO InstrumentType (id, Name) VALUES (4, 'Ndf');
INSERT INTO InstrumentType (id, Name) VALUES (5, 'Cds');



/* Data for table Isin */




/* Data for table Leg */




/* Data for table LegType */
INSERT INTO LegType (id, Name) VALUES (1, 'Received');
INSERT INTO LegType (id, Name) VALUES (2, 'Paid');
INSERT INTO LegType (id, Name) VALUES (3, 'Both');



/* Data for table LegalEntity */




/* Data for table Rating */
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (1, 210, 'AAA', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (2, 200, 'AA+', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (3, 190, 'AA', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (4, 180, 'AA-', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (5, 170, 'A+', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (6, 160, 'A', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (7, 150, 'A-', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (8, 140, 'BBB+', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (9, 130, 'BBB', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (10, 120, 'BBB-', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (11, 110, 'BB+', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (12, 100, 'BB', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (13, 90, 'BB-', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (14, 80, 'B+', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (15, 70, 'B', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (16, 60, 'B-', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (17, 50, 'CCC+', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (18, 40, 'CCC', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (19, 30, 'CCC-', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (20, 20, 'CC', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (21, 10, 'C', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (22, 0, '', 1);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (23, 210, 'AAA', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (24, 200, 'AA+', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (25, 190, 'AA', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (26, 180, 'AA-', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (27, 170, 'A+', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (28, 160, 'A', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (29, 150, 'A-', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (30, 140, 'BBB+', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (31, 130, 'BBB', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (32, 120, 'BBB-', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (33, 110, 'BB+', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (34, 100, 'BB', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (35, 90, 'BB-', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (36, 80, 'B+', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (37, 70, 'B', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (38, 60, 'B-', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (39, 50, 'CCC+', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (40, 40, 'CCC', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (41, 30, 'CCC-', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (42, 20, 'CC', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (43, 10, 'C', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (44, 0, '', 3);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (45, 210, 'AAA', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (46, 200, 'Aa1', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (47, 190, 'Aa2', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (48, 180, 'Aa3', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (49, 170, 'A1', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (50, 160, 'A2', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (51, 150, 'A3', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (52, 140, 'Baa1', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (53, 130, 'Baa2', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (54, 120, 'Baa3', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (55, 110, 'Ba1', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (56, 100, 'Ba2', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (57, 90, 'Ba3', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (58, 80, 'B1', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (59, 70, 'B2', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (60, 60, 'B3', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (61, 50, 'Caa1', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (62, 40, 'Caa2', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (63, 30, 'Caa3', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (64, 20, 'Ca', 2);
INSERT INTO Rating (id, Value, Name, id_RatingAgency) VALUES (65, 0, '', 2);



/* Data for table RatingAgency */
INSERT INTO RatingAgency (id, Name, Description) VALUES (1, 'S&P', 'Standard and Poors');
INSERT INTO RatingAgency (id, Name, Description) VALUES (2, 'Moody''s', 'Moody''s rating agency');
INSERT INTO RatingAgency (id, Name, Description) VALUES (3, 'Fitch', 'Fitch rating agency');



/* Data for table RatingAgencyCode */
INSERT INTO RatingAgencyCode (id, Name, id_RatingAgency) VALUES (1, 'S&P', 1);
INSERT INTO RatingAgencyCode (id, Name, id_RatingAgency) VALUES (2, 'SPI', 1);
INSERT INTO RatingAgencyCode (id, Name, id_RatingAgency) VALUES (3, 'MDL', 2);
INSERT INTO RatingAgencyCode (id, Name, id_RatingAgency) VALUES (4, 'MIS', 2);
INSERT INTO RatingAgencyCode (id, Name, id_RatingAgency) VALUES (5, 'MDY', 2);
INSERT INTO RatingAgencyCode (id, Name, id_RatingAgency) VALUES (6, 'FTC', 3);
INSERT INTO RatingAgencyCode (id, Name, id_RatingAgency) VALUES (7, 'FDL', 3);
INSERT INTO RatingAgencyCode (id, Name, id_RatingAgency) VALUES (8, 'FSU', 3);



/* Data for table RatingToInstrument */




/* Data for table RatingToLegalEntity */




/* Data for table Ric */




/* Data for table RicToChain */




/* Data for table Seniority */




/* Data for table Specimen */




/* Data for table SubIndustry */




/* Data for table Ticker */




/* Data for table _log */
