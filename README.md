---
 
---

# PinusDB.Data

Ϊ�����ɹ�ʱ�����ݿ�(pinusdb)ʵ�ֵı�׼ADO.Net �����ݷ��ʽӿڡ� 

PinusDB.Data ��ͬ��[�ٷ��ṩ�� .Net SDK](https://gitee.com/pinusdb/pinusdb/blob/master/doc/pinusdb_dotnet_sdk.md)  , �������ADO.Net��׼��


---

[![Build status](https://ci.appveyor.com/api/projects/status/6t81vxhmm2rpthol?svg=true)](https://ci.appveyor.com/project/MaiKeBing/pinusdb-data)
[![License](https://img.shields.io/github/license/maikebing/PinusDB.Data.svg)](https://github.com/maikebing/PinusDB.Data/blob/master/LICENSE)

| NuGet����    | �汾|������| ˵��                                                     |
| ----------- | --------  | --------  | ------------------------------------------------------------ |
| PinusDB.Data |[![PinusDB](https://img.shields.io/nuget/v/PinusDB.svg)](https://www.nuget.org/packages/PinusDB/) |![Nuget](https://img.shields.io/nuget/dt/PinusDB) |ADO.Net Core �������
| PinusDB.HealthChecks |[![PinusDB.HealthChecks](https://img.shields.io/nuget/v/PinusDB.HealthChecks.svg)](https://www.nuget.org/packages/PinusDB.HealthChecks/)  |  ![Nuget](https://img.shields.io/nuget/dt/PinusDB.HealthChecks)| ��Asp.Net Core ʹ�õĽ���������


ʾ������:

```c#
   var builder = new PinusConnectionStringBuilder()
                {
                    Server = "127.0.0.1",
                    Username = "sa",
                    Password = "future",
                    Port = 8105
                };
                using (var connection = new PinusConnection(builder.ConnectionString))
                {
                    connection.Open();
                    Console.WriteLine("����ң�����ݱ�", connection.CreateCommand(
                        @" CREATE TABLE test (  devid bigint,                  tstamp datetime, val01 bool, val02 bigint, val03 double, val04 real2 )").ExecuteNonQuery());
                    Console.WriteLine("�����豸", connection.CreateCommand(@"INSERT INTO sys_dev(tabname, devid) VALUES('test',1)").ExecuteNonQuery());
                    Console.WriteLine("�������", connection.CreateCommand(@"INSERT INTO test(devid,tstamp,val01,val02,val03) VALUES(1, now(), true, 1, 1.1111)").ExecuteNonQuery());
                    var cmd_select = connection.CreateCommand();
                    cmd_select.CommandText = $"SELECT * FROM test";
                    var reader = cmd_select.ExecuteReader();
                    Console.WriteLine(cmd_select.CommandText);
                    Console.WriteLine("��ѯ����");
                    ConsoleTableBuilder.From(reader.ToDataTable()).WithFormat(ConsoleTableBuilderFormat.MarkDown).ExportAndWriteLine();
                    Console.WriteLine("");
                    Console.WriteLine("ɾ����", connection.CreateCommand($"DROP TABLE test").ExecuteNonQuery());
                    Console.WriteLine("ɾ���豸", connection.CreateCommand($"DELETE FROM sys_dev WHERE tabname='test'").ExecuteNonQuery());
                    connection.Close();
                }
```



 ![img1](docs/img1.jpg)