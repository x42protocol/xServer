import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { FullNodeApiService } from '../../shared/services/fullnode.api.service';

export class SmartContractsContractItem {
    constructor(public blockId: string, public type: string, public hash: string, public destinationAddress: string, public amount: number) { }
}

export class ContractTransactionItem
{
    blockHeight: number;
    type: number;
    hash: string;
    to: string;
    amount: number;
}

export abstract class SmartContractsServiceBase {
    GetReceipt(hash: string): Observable<string> { return of(); }
    GetAddresses(walletName: string): Observable<string[]> { return of(); }
    GetBalance(walletName: string): Observable<number> { return of(); }
    GetAddressBalance(address: string): Observable<number> { return of(); }
    GetAddress(walletName: string): Observable<string> { return of(); }
    GetContracts(walletName: string): Observable<SmartContractsContractItem[]> { return of(); }
    GetSenderAddresses(walletName: string): Observable<string[]> { return of(); }
    GetParameterTypes(walletName: string): Observable<string[]> { return of(); }
    GetHistory(walletName: string, address: string): Observable<ContractTransactionItem[]> { return of(); }
    PostCreate(createTransaction: any): Observable<any> { return of(); }
    PostCall(createTransaction: any): Observable<any> { return of(); }
}

@Injectable()
export class SmartContractsService implements SmartContractsServiceBase
{
    constructor(private FullNodeApiService: FullNodeApiService) { }

    GetReceipt(hash: string): Observable<string> {
        return this.FullNodeApiService.getReceipt(hash)
    }

    PostCall(createTransaction: any): Observable<any> {
        return this.FullNodeApiService.postCallTransaction(createTransaction)
    }

    PostCreate(createTransaction: any): Observable<any> {
        return this.FullNodeApiService.postCreateTransaction(createTransaction)
    }

    GetHistory(walletName: string, address: string): Observable<any> {
        return this.FullNodeApiService.getAccountHistory(walletName, address)
    }

    GetBalance(walletName: string): Observable<any> {
        return this.FullNodeApiService.getAccountBalance(walletName)
    }

    GetAddressBalance(address: string): Observable<any> {
        return this.FullNodeApiService.getAddressBalance(address)
    }

    GetAddress(walletName: string): Observable<any> {
        return this.FullNodeApiService.getAccountAddress(walletName)
    }

    GetAddresses(walletName: string): Observable<any> {
        return this.FullNodeApiService.getAccountAddresses(walletName)
    }

    GetContracts(walletName: string): Observable<SmartContractsContractItem[]> {
        return of([
            new SmartContractsContractItem('7809', 'Transfert', 'bbdbcae72f1085710', 'SdrP9wvxZmaG7t3UAjxxyB6RNT9FV1Z2Sn', 10898025),
            new SmartContractsContractItem('7810', 'Transfert', 'bbdbcae72f1085710', 'SdrP9wvxZmaG7t3UAjxxyB6RNT9FV1Z2Sn', 11898025),
            new SmartContractsContractItem('7811', 'Transfert', 'bbdbcae72f1085710', 'SdrP9wvxZmaG7t3UAjxxyB6RNT9FV1Z2Sn', 12898025),
            new SmartContractsContractItem('7812', 'Transfert', 'bbdbcae72f1085710', 'SdrP9wvxZmaG7t3UAjxxyB6RNT9FV1Z2Sn', 13898025),
            new SmartContractsContractItem('7813', 'Transfert', 'bbdbcae72f1085710', 'SdrP9wvxZmaG7t3UAjxxyB6RNT9FV1Z2Sn', 14898025),
            new SmartContractsContractItem('7814', 'Transfert', 'bbdbcae72f1085710', 'SdrP9wvxZmaG7t3UAjxxyB6RNT9FV1Z2Sn', 15898025),
        ]);
    }

    GetSenderAddresses(walletName: string): Observable<string[]> {
        return of([
            'SarP7wvxZmaG7t1UAjaxyB6RNT9FV1Z2Sn',
            'SbrP8wvxZmaG7t2UAjbxyB7RNT9FV1Z2Sn',
            'ScrP9wvxZmaG7t3UAjcxyB8RNT9FV1Z2Sn'
        ]);
    }

    GetParameterTypes(walletName: string): Observable<string[]> {
        return of([
            'Type 1',
            'Type 2',
            'Type 3'
        ]);
    }
}
