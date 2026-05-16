# MSAP Data Model Reference (Verified)

This document provides a verified overview of the tables and their relationships for the MSAP database.

## Key Conventions
- **PK**: Primary Key. Most tables use `RECID` as a unique surrogate key.
- **UK**: Unique Key. Often a business ID like `NUMBER` or `CODE`.
- **FK**: Foreign Key. References another table's PK or UK.
- **IDs**: Business IDs (CUSTNO, VESSELNUM, etc.) are typically zero-padded strings (e.g., `0001`).

## Master Data Tables
- **customer**: `RECID` (PK), `NUMBER` (UK), `NAME`, `ADDRESS1/2/3`, `TIN`, `TERMS`, `VATABLE` (Boolean)
- **vessel**: `RECID` (PK), `NUMBER` (UK), `NAME`, `TYPE` (`F`=Foreign, `L`=Local)
- **port**: `RECID` (PK), `NUMBER` (UK), `NAME`
- **terminal**: `RECID` (PK), `NUMBER` (UK), `NAME`
- **tugboat**: `RECID` (PK), `NUMBER` (UK), `NAME`, `OWNER`
- **tugmaster**: `RECID` (PK), `EMPNO` (UK), `NAME`, `ACTIVE` (Boolean)
- **userfile**: `USERID` (PK), `USERNAME`, `FULLNAME`, `ADMIN` (Boolean)
- **bankacct**: `RECID` (PK), `CODE` (UK), `BANK`, `ACCOUNTNO`, `ACCOUNTNAM`
- **principal**: `RECID` (PK), `NUMBER` (UK), `NAME`, `AGENT`, `ADDRESS1/2/3`

## Transactional Tables
- **billing**: `RECID` (PK), `NUMBER` (UK*), `DATE`, `CUSTNO` (FK->customer.NUMBER), `VESSELNUM` (FK->vessel.NUMBER), `PORTNUM` (FK->port.NUMBER), `AMOUNT`, `VAT` (Boolean), `CRNUM` (FK->collection.CRNUM), `TERMINAL` (FK->terminal.NUMBER)
    - *Note: One duplicate `NUMBER` (3027) was found in data, suggesting `RECID` is the only safe PK.*
- **dispatch**: `RECID` (PK), `NUMBER` (UK), `DATE`, `VESSELNUM` (FK->vessel.NUMBER), `PORTNUM` (FK->port.NUMBER), `TUGNUM` (FK->tugboat.NUMBER), `CUSTNO` (FK->customer.NUMBER), `MASTERNO` (FK->tugmaster.EMPNO), `BILLNUM` (FK->billing.NUMBER), `TERMINAL` (FK->terminal.NUMBER)
- **collection**: `RECID` (PK), `CRNUM` (UK), `CRDATE`, `CUSTNO` (FK->customer.NUMBER), `AMOUNT`, `BANKACCTCO` (FK->bankacct.CODE), `CHECKNO`, `CHECKDATE`
- **collect_bill**: `RECID` (PK), `CRNUM` (FK->collection.CRNUM), `BILLNUM` (FK->billing.NUMBER), `BILLCUST` (FK->customer.NUMBER)
- **bill_adjust**: `RECID` (PK), `BILLNUM` (FK->billing.NUMBER), `DISPATCHNU` (FK->dispatch.NUMBER), `RATE`, `AMOUNT`
- **bill_dispatch**: `RECID` (PK), `BILLNUM` (FK->billing.NUMBER), `DISPATCHNU` (FK->dispatch.NUMBER), `RATE`, `AMOUNT`, `APOTHERTUG`
- **atrail**: `RECID` (PK), `USERID` (FK->userfile.USERID), `DATE`, `ACTIVITY`, `FILE` (Table Name), `RECORD_ID` (FK->TargetTable.RECID)
- **attach**: `RECID` (PK), `DISPATCHNU` (FK->dispatch.NUMBER), `FILENAME`, `SOURCE`

## Configuration / Reference Tables
- **tariff**: `RECID` (PK), `DATE`, `CUSTNO` (FK->customer.NUMBER), `PORT` (FK->port.NUMBER), `TERMINAL`, `SERVICE`, `DISPATCH`, `BAF`
- **rates**: `RECID` (PK), `TYPE`, `AMOUNT`, `ASOF`
- **services**: `RECID` (PK), `NUMBER` (UK), `TYPE`, `DESC`
- **module**: `NUM` (PK), `MODNAME`, `DESCRIPTIO`
- **useraccess**: `USERID` (FK->userfile.USERID), `TYPE`, `ACCESS`, `ALLOWED`

## Relationship Integrity (Verification Results)
- `billing.CUSTNO` -> `customer.NUMBER`: 100% Match.
- `dispatch.CUSTNO` -> `customer.NUMBER`: 100% Match.
- `dispatch.VESSELNUM` -> `vessel.NUMBER`: 100% Match.
- `dispatch.TUGNUM` -> `tugboat.NUMBER`: 100% Match.
- `collect_bill.CRNUM` -> `collection.CRNUM`: 100% Match.
- `bill_dispatch.DISPATCHNU` -> `dispatch.NUMBER`: >99% Match (Few exceptions with `Bt24-` prefixes).
- `billing.PORTNUM` -> `port.NUMBER`: Partial Match (Some ports referenced in transactions are not in the master list).
- `billing.VESSELNUM` -> `vessel.NUMBER`: >99% Match (Few vessels missing from master list).
