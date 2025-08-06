#import "IOSShare.h"
#import <UIKit/UIKit.h>

// C function called from Unity
extern "C" {
    void _ShareFile(const char* filePathCStr, const char* messageCStr) {
        NSString *filePath = [NSString stringWithUTF8String:filePathCStr];
        NSString *message = [NSString stringWithUTF8String:messageCStr];
        [IOSShare shareFile:filePath message:message];
    }
}

@implementation IOSShare

+ (void)shareFile:(NSString *)filePath message:(NSString *)message {
    // Create URL from file path
    NSURL *fileURL = [NSURL fileURLWithPath:filePath];

    // Array of items to share
    NSMutableArray *activityItems = [NSMutableArray arrayWithObject:fileURL];
    if (message && message.length > 0) {
        [activityItems insertObject:message atIndex:0];
    }

    // Create an instance of UIActivityViewController
    UIActivityViewController *activityViewController = [[UIActivityViewController alloc] initWithActivityItems:activityItems applicationActivities:nil];

    // Optional: Exclude activity types
    // Example: UIActivityTypePostToWeibo, UIActivityTypeAddToReadingList, etc.
    activityViewController.excludedActivityTypes = @[];

    // Get the current ViewController to present the share sheet
    UIViewController *currentViewController = nil;
    
    // Handle multiple scenes for iOS 13 and later
    if (@available(iOS 13.0, *)) {
        // Search for the active scene's UIWindow
        for (UIWindowScene *scene in [UIApplication sharedApplication].connectedScenes) {
            if (scene.activationState == UISceneActivationStateForegroundActive) {
                for (UIWindow *window in scene.windows) {
                    if (window.isKeyWindow) { // This keyWindow refers to the primary window within that specific scene
                        currentViewController = window.rootViewController;
                        break;
                    }
                }
            }
            if (currentViewController) {
                break;
            }
        }
    }
    
    // Fallback for iOS 12 and earlier, or if not found in iOS 13+ (uses deprecated keyWindow)
    if (!currentViewController) {
        currentViewController = [UIApplication sharedApplication].keyWindow.rootViewController;
    }
    
    // Traverse through presented view controllers to find the topmost one
    while (currentViewController.presentedViewController) {
        currentViewController = currentViewController.presentedViewController;
    }

    [currentViewController presentViewController:activityViewController animated:YES completion:nil];

    // iPad crash prevention (PopOverPresentationController setup)
    // On iPad, UIActivityViewController might crash if PopOverPresentationController is not configured.
    if ([UIDevice currentDevice].userInterfaceIdiom == UIUserInterfaceIdiomPad) {
        activityViewController.popoverPresentationController.sourceView = currentViewController.view;
        // Optional: Specify sourceRect for precise positioning
        // activityViewController.popoverPresentationController.sourceRect = CGRectMake(currentViewController.view.bounds.size.width / 2, currentViewController.view.bounds.size.height / 2, 0, 0);
        activityViewController.popoverPresentationController.permittedArrowDirections = 0; // No arrow
    }
}

@end