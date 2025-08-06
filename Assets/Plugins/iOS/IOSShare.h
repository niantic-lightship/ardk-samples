#import <Foundation/Foundation.h>

@interface IOSShare : NSObject

+ (void)shareFile:(NSString *)filePath message:(NSString *)message;

@end