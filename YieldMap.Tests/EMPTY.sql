/* Data for table Feed */
INSERT INTO Feed (id, Name, Description) VALUES (1, 'Q', 'Main Eikon data feed');

/* Data for table Field */
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (1, 'BID', 1, 1);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (2, 'ASK', 1, 2);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (3, 'LAST', 1, 3);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (4, 'CLOSE', 1, 4);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (5, 'VWAP', 1, 5);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (6, 'VOLUME', 1, 6);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (7, '393', 2, 1);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (8, '275', 2, 2);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (9, 'CLOSE', 2, 4);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (10, '1053', 3, 3);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (11, 'CLOSE', 3, 4);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (12, '393', 4, 1);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (13, 'CLOSE', 4, 4);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (15, '275', 5, 2);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (16, '21', 5, 4);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (17, '1051', 5, 9);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (18, '1003', 4, 8);
INSERT INTO Field (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (19, '393', 5, 1);

/* Data for table FieldDefinition */
INSERT INTO FieldDefinition (id, Name) VALUES (1, 'BID');
INSERT INTO FieldDefinition (id, Name) VALUES (2, 'ASK');
INSERT INTO FieldDefinition (id, Name) VALUES (3, 'LAST');
INSERT INTO FieldDefinition (id, Name) VALUES (4, 'CLOSE');
INSERT INTO FieldDefinition (id, Name) VALUES (5, 'VWAP');
INSERT INTO FieldDefinition (id, Name) VALUES (6, 'VOLUME');
INSERT INTO FieldDefinition (id, Name) VALUES (7, 'VALUE');
INSERT INTO FieldDefinition (id, Name) VALUES (8, 'TENOR');
INSERT INTO FieldDefinition (id, Name) VALUES (9, 'MATURITY');

/* Data for table FieldGroup */
INSERT INTO FieldGroup (id, Name, "Default", id_DefaultFieldDef) VALUES (1, 'Micex', 0, NULL);
INSERT INTO FieldGroup (id, Name, "Default", id_DefaultFieldDef) VALUES (2, 'Eurobonds', 1, 1);
INSERT INTO FieldGroup (id, Name, "Default", id_DefaultFieldDef) VALUES (3, 'Russian CPI Index', 0, NULL);
INSERT INTO FieldGroup (id, Name, "Default", id_DefaultFieldDef) VALUES (4, 'Mosprime', 0, NULL);
INSERT INTO FieldGroup (id, Name, "Default", id_DefaultFieldDef) VALUES (5, 'Swaps', 0, NULL);

/* Data for table InstrumentType */
INSERT INTO InstrumentType (id, Name) VALUES (1, 'Bond');
INSERT INTO InstrumentType (id, Name) VALUES (2, 'Frn');
INSERT INTO InstrumentType (id, Name) VALUES (3, 'Swap');
INSERT INTO InstrumentType (id, Name) VALUES (4, 'Ndf');
INSERT INTO InstrumentType (id, Name) VALUES (5, 'Cds');

/* Data for table LegType */
INSERT INTO LegType (id, Name) VALUES (1, 'Received');
INSERT INTO LegType (id, Name) VALUES (2, 'Paid');
INSERT INTO LegType (id, Name) VALUES (3, 'Both');

/* Data for table Property */
INSERT INTO Property (id, Name, Description, Expression, id_InstrumentTpe) VALUES (1, 'Issuer-Series', 'Label', '$Name + \" \" + $Series', 1);
INSERT INTO Property (id, Name, Description, Expression, id_InstrumentTpe) VALUES (2, 'Issuer-Coupon-Maturity', 'Label', '$Name + IIf(Not IsNothing($Coupon), \" \" + Format(\"0.00\", $Coupon), \"\") + IIf(Not IsNothing($Maturity), \" ''\" + Format(\"MMM-yy\", $Maturity), \"\")', 1);

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