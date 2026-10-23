// iOS share sheet for Arrows Escape (Step 14N). Called from NativeShare.cs through [DllImport("__Internal")].
#import <UIKit/UIKit.h>

extern UIViewController* UnityGetGLViewController();

extern "C" void ArrowEscapeShareText(const char* text)
{
    if (text == NULL) return;
    NSString* message = [NSString stringWithUTF8String:text];
    UIActivityViewController* sheet = [[UIActivityViewController alloc] initWithActivityItems:@[message] applicationActivities:nil];
    UIViewController* root = UnityGetGLViewController();
    if (sheet.popoverPresentationController != nil)   // iPad: anchor the popover to the centre of the screen
    {
        sheet.popoverPresentationController.sourceView = root.view;
        sheet.popoverPresentationController.sourceRect = CGRectMake(root.view.bounds.size.width / 2, root.view.bounds.size.height / 2, 1, 1);
        sheet.popoverPresentationController.permittedArrowDirections = 0;
    }
    [root presentViewController:sheet animated:YES completion:nil];
}
