INSERT INTO "RatingAgency"("id", "Name", "Description") VALUES ('1', 'S&P', 'Standard and Poors'), ('2', 'Moody''s', 'Moody''s rating agency'), ('3', 'Fitch', 'Fitch rating agency')
INSERT INTO "Feed"("id", "Name", "Description") VALUES ('1', 'Q', 'Main Eikon data feed')
INSERT INTO "RatingAgencyCode"("id", "Name", "id_RatingAgency") VALUES ('1', 'S&P', '1'), ('2', 'SPI', '1'), ('3', 'MDL', '2'), ('4', 'MIS', '2'), ('5', 'MDY', '2'), ('6', 'FTC', '3'), ('7', 'FDL', '3'), ('8', 'FSU', '3')
INSERT INTO "_log"("msg") VALUES ('HAHA'), ('HAHA'), ('HAHA'), ('HAHA'), ('HAHA'), ('HAHA'), ('OHLOH'), ('OHLOH'), ('HAHA'), ('OHLOH'), ('OHLOH'), ('OHLOH'), ('HAHA'), ('OHLOH'), ('OHLOH'), ('HAHA'), ('HAHA'), ('OHLOH'), ('OHLOH'), ('Trying to remove last default'), ('Setting new default'), ('Trying to remove last default')
INSERT INTO "Rating"("id", "Value", "Name", "id_RatingAgency") VALUES ('1', '210', 'AAA', '1'), ('2', '200', 'AA+', '1'), ('3', '190', 'AA', '1'), ('4', '180', 'AA-', '1'), ('5', '170', 'A+', '1'), ('6', '160', 'A', '1'), ('7', '150', 'A-', '1'), ('8', '140', 'BBB+', '1'), ('9', '130', 'BBB', '1'), ('10', '120', 'BBB-', '1'), ('11', '110', 'BB+', '1'), ('12', '100', 'BB', '1'), ('13', '90', 'BB-', '1'), ('14', '80', 'B+', '1'), ('15', '70', 'B', '1'), ('16', '60', 'B-', '1'), ('17', '50', 'CCC+', '1'), ('18', '40', 'CCC', '1'), ('19', '30', 'CCC-', '1'), ('20', '20', 'CC', '1'), ('21', '10', 'C', '1'), ('22', '0', '', '1'), ('23', '210', 'AAA', '3'), ('24', '200', 'AA+', '3'), ('25', '190', 'AA', '3'), ('26', '180', 'AA-', '3'), ('27', '170', 'A+', '3'), ('28', '160', 'A', '3'), ('29', '150', 'A-', '3'), ('30', '140', 'BBB+', '3'), ('31', '130', 'BBB', '3'), ('32', '120', 'BBB-', '3'), ('33', '110', 'BB+', '3'), ('34', '100', 'BB', '3'), ('35', '90', 'BB-', '3'), ('36', '80', 'B+', '3'), ('37', '70', 'B', '3'), ('38', '60', 'B-', '3'), ('39', '50', 'CCC+', '3'), ('40', '40', 'CCC', '3'), ('41', '30', 'CCC-', '3'), ('42', '20', 'CC', '3'), ('43', '10', 'C', '3'), ('44', '0', '', '3'), ('45', '210', 'AAA', '2'), ('46', '200', 'Aa1', '2'), ('47', '190', 'Aa2', '2'), ('48', '180', 'Aa3', '2'), ('49', '170', 'A1', '2'), ('50', '160', 'A2', '2'), ('51', '150', 'A3', '2'), ('52', '140', 'Baa1', '2'), ('53', '130', 'Baa2', '2'), ('54', '120', 'Baa3', '2'), ('55', '110', 'Ba1', '2'), ('56', '100', 'Ba2', '2'), ('57', '90', 'Ba3', '2'), ('58', '80', 'B1', '2'), ('59', '70', 'B2', '2'), ('60', '60', 'B3', '2'), ('61', '50', 'Caa1', '2'), ('62', '40', 'Caa2', '2'), ('63', '30', 'Caa3', '2'), ('64', '20', 'Ca', '2'), ('65', '0', '', '2')
INSERT INTO "LegType"("id", "Name") VALUES ('1', 'Received'), ('2', 'Paid'), ('3', 'Both')
INSERT INTO "InstrumentType"("id", "Name") VALUES ('1', 'Bond'), ('2', 'Frn'), ('3', 'Swap'), ('4', 'Ndf'), ('5', 'Cds')
INSERT INTO "Ric"("id", "Name", "id_Isin", "id_Feed", "id_FieldGroup") VALUES ('1', 'RUCPI=ECI', NULL, '1', '3'), ('2', 'MOSPRIME3MD=', NULL, '1', '4'), ('3', 'RUSSRR', NULL, '1', '1')
INSERT INTO "Index"("id", "Name", "id_Ric") VALUES ('1', 'RUCPI1M', '1'), ('2', 'RUSSRR', '3'), ('3', 'MPRIME3M', '2')
/* Data for table Field */
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (1, 'BID', 1, 1);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (2, 'ASK', 1, 2);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (3, 'LAST', 1, 3);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (4, 'CLOSE', 1, 4);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (5, 'VWAP', 1, 5);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (6, 'VOLUME', 1, 6);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (7, '393', 2, 1);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (8, '275', 2, 2);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (9, 'CLOSE', 2, 4);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (10, '1053', 3, 3);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (11, 'CLOSE', 3, 4);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (12, '393', 4, 1);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (13, 'CLOSE', 4, 4);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (15, '275', 5, 2);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (16, '21', 5, 4);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (17, '1051', 5, 9);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (18, '1003', 4, 8);
INSERT INTO "Field" (id, SystemName, id_FieldGroup, id_FieldDef) VALUES (19, '393', 5, 1);
/* Data for table FieldDefinition */
INSERT INTO "FieldDefinition" (id, Name) VALUES (1, 'BID');
INSERT INTO "FieldDefinition" (id, Name) VALUES (2, 'ASK');
INSERT INTO "FieldDefinition" (id, Name) VALUES (3, 'LAST');
INSERT INTO "FieldDefinition" (id, Name) VALUES (4, 'CLOSE');
INSERT INTO "FieldDefinition" (id, Name) VALUES (5, 'VWAP');
INSERT INTO "FieldDefinition" (id, Name) VALUES (6, 'VOLUME');
INSERT INTO "FieldDefinition" (id, Name) VALUES (7, 'VALUE');
INSERT INTO "FieldDefinition" (id, Name) VALUES (8, 'TENOR');
INSERT INTO "FieldDefinition" (id, Name) VALUES (9, 'MATURITY');
/* Data for table FieldGroup */
INSERT INTO "FieldGroup" (id, Name, "Default", id_DefaultFieldDef) VALUES (1, 'Micex', 0, NULL);
INSERT INTO "FieldGroup" (id, Name, "Default", id_DefaultFieldDef) VALUES (2, 'Eurobonds', 1, 1);
INSERT INTO "FieldGroup" (id, Name, "Default", id_DefaultFieldDef) VALUES (3, 'Russian CPI Index', 0, NULL);
INSERT INTO "FieldGroup" (id, Name, "Default", id_DefaultFieldDef) VALUES (4, 'Mosprime', 0, NULL);
INSERT INTO "FieldGroup" (id, Name, "Default", id_DefaultFieldDef) VALUES (5, 'Swaps', 0, NULL);