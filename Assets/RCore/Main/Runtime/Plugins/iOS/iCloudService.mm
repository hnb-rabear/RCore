#import <Foundation/Foundation.h>
#import <CloudKit/CloudKit.h>

@interface iCloudService : NSObject
+ (void)saveStringToiCloud:(NSString *)key value:(NSString *)value completion:(void (^)(NSError *error))completion;
+ (void)retrieveStringFromiCloud:(NSString *)key completion:(void (^)(NSString *value, NSError *error))completion;
+ (void)checkiCloudAuthentication:(void (^)(BOOL authenticated, NSString *error))completion;
@end

@implementation iCloudService

+ (void)saveStringToiCloud:(NSString *)key value:(NSString *)value completion:(void (^)(NSError *error))completion {
    CKContainer *container = [CKContainer defaultContainer];
    CKDatabase *database = [container privateCloudDatabase];
    
    CKRecordID *recordID = [[CKRecordID alloc] initWithRecordName:key];
    CKRecord *record = [[CKRecord alloc] initWithRecordType:@"StringRecord" recordID:recordID];
    record[@"value"] = value;
    
    [database saveRecord:record completionHandler:^(CKRecord *record, NSError *error) {
        if (error) {
            NSLog(@"Error saving to iCloud: %@", error.localizedDescription);
        } else {
            NSLog(@"Successfully saved to iCloud");
        }
        if (completion) {
            completion(error);
        }
    }];
}

+ (void)retrieveStringFromiCloud:(NSString *)key completion:(void (^)(NSString *value, NSError *error))completion {
    CKContainer *container = [CKContainer defaultContainer];
    CKDatabase *database = [container privateCloudDatabase];
    
    CKRecordID *recordID = [[CKRecordID alloc] initWithRecordName:key];
    
    [database fetchRecordWithID:recordID completionHandler:^(CKRecord *record, NSError *error) {
        if (error) {
            NSLog(@"Error retrieving from iCloud: %@", error.localizedDescription);
            if (completion) {
                completion(nil, error);
            }
        } else {
            NSString *retrievedValue = record[@"value"];
            if (completion) {
                completion(retrievedValue, nil);
            }
        }
    }];
}

+ (void)checkiCloudAuthentication:(void (^)(BOOL authenticated, NSString *error))completion {
    CKContainer *container = [CKContainer defaultContainer];
    
    [container accountStatusWithCompletionHandler:^(CKAccountStatus accountStatus, NSError *error) {
        BOOL isAuthenticated = (accountStatus == CKAccountStatusAvailable);
        NSString *errorString = error ? error.localizedDescription : nil;
        if (completion) {
            completion(isAuthenticated, errorString);
        }
    }];
}

@end