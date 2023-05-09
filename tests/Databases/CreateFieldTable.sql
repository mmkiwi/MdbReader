CREATE TABLE TestAllFieldTypes (
  Id               COUNTER,
  BoolColumn       BIT,
  ByteColumn       BYTE           NULL,
  IntColumn        INT            NULL,
  LongIntColumn    LONG           NULL,
  MoneyColumn      CURRENCY       NULL,
  FloatColumn      SINGLE         NULL,
  DoubleColumn     DOUBLE         NULL,
  DateTimeColumn   DATETIME       NULL,
  VarbinaryColumn  VARBINARY(100) NULL, 
  BinaryColumn     BINARY(100)    NULL, 
  OLEColumn        LONGBINARY     NULL,
  VarCharColumn    VARCHAR(80)    NULL,
  CharColumn       CHAR(20)       NULL,
  MemoColumn       LONGTEXT     NULL,
  RepIdColumn      GUID           NULL,
  Comment          VARCHAR(80)    NULL,
  CONSTRAINT pkTestAllFieldTypes PRIMARY KEY (Id)
);

INSERT INTO TestAllFieldTypes
  (Comment)
VALUES 
  ('Null for all values');

INSERT INTO TestAllFieldTypes
  (BoolColumn, ByteColumn, IntColumn, LongIntColumn, MoneyColumn, FloatColumn, DoubleColumn, RepIdColumn, DateTimeColumn, Comment)
VALUES 
  (TRUE      , 250       , 12600    , 12685031     , 1882.01    , 1532.1212  , 589962.14698,
  '{97ffe876-7114-4e00-9379-05512f2b6492}',
   '2020-04-04 12:30:44 PM',
   'Positive numbers');

INSERT INTO TestAllFieldTypes
  (BoolColumn, ByteColumn, IntColumn, LongIntColumn, MoneyColumn, FloatColumn, DoubleColumn, RepIdColumn, DateTimeColumn, Comment)
VALUES 
  (TRUE      , -50       , -45265    , -5683421     , -6533.21   , -2573.589  , -3579994.258,
  '{281b90a5-3dd0-431d-9f39-61a53b58e9f9}',
  '700-04-04 12:30:44 PM',
  'Negative numbers');

  
INSERT INTO TestAllFieldTypes
  (VarbinaryColumn, BinaryColumn, VarCharColumn, CharColumn, MemoColumn, RepIdColumn, Comment)
VALUES 
  ('TestAscii', 'TestSmall', 'TestSmallish', 'TestSmallHey', 'TestInline',
  '{a877ef6b-da8b-4824-bc70-35e71fc5de21}',
  'Small ASCII Binary');

INSERT INTO TestAllFieldTypes
  (VarbinaryColumn, BinaryColumn, RepIdColumn, Comment)
VALUES 
  (0x98db72bedf158b4fae662263efcb1918,
   0x126b21c95dd71d3635ab20b359ce6517,
   '{ecf8abbb-8b70-4bc1-884d-991e7a1e8db6}',
   'Small Non-ASCII Binary');

INSERT INTO TestAllFieldTypes
  (RepIdColumn, Comment)
VALUES 
  (
   '{8dfbceb8-58a2-4a92-8439-8dc98097a1ae}',
   'Large Text');

INSERT INTO TestAllFieldTypes
  (RepIdColumn, Comment)
VALUES 
  (
   '{63138e61-78e2-4d7c-b244-1bd6d8009cc5}',
   'Medium Binary');

INSERT INTO TestAllFieldTypes
  (RepIdColumn, Comment)
VALUES 
  (
   '{63138e61-78e2-4d7c-b244-1bd6d8009cc5}',
   'Large Binary');